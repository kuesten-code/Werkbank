using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Kuestencode.Faktura.Controllers;
using Kuestencode.Faktura.Data;
using Kuestencode.Faktura.Models;
using Kuestencode.Faktura.Tests.TestDoubles;
using Kuestencode.Shared.Contracts.Faktura;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kuestencode.Faktura.Tests;

/// <summary>
/// Endpunkte, die andere Module konsumieren (Saldo: EÜR-Zahlungen, Acta: Projektsummen) sowie Filter,
/// Audit-Log und Versand. Eigene Factory-Instanz = eigene InMemory-DB, damit Summen nicht durch Daten
/// anderer Testklassen verfälscht werden.
/// </summary>
public class InvoiceControllerReportingTests : IClassFixture<FakturaWebApplicationFactory>
{
    private readonly FakturaWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public InvoiceControllerReportingTests(FakturaWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.Create("Admin"));
    }

    private static DateTime Utc(int year, int month, int day) => new(year, month, day, 0, 0, 0, DateTimeKind.Utc);

    private async Task<Invoice> SeedAsync(Invoice invoice)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FakturaDbContext>();
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();
        return invoice;
    }

    private static Invoice MakeInvoice(string number, InvoiceStatus status = InvoiceStatus.Sent, int customerId = 1,
        int? projectId = null, InvoiceType type = InvoiceType.Invoice, decimal unitPrice = 1000m) => new()
    {
        InvoiceNumber = number,
        InvoiceDate = Utc(2025, 1, 10),
        CustomerId = customerId,
        ProjectId = projectId,
        Status = status,
        Type = type,
        Items = [new InvoiceItem { Description = "Leistung", Quantity = 1, UnitPrice = unitPrice, VatRate = 19 }]
    };

    // ─── EÜR-Zahlungen (Saldo) ────────────────────────────────────────────────

    [Fact]
    public async Task EuerPayments_JedeTeilzahlungImZeitraumEinzelnMitPositionenUndKundenname()
    {
        var invoice = MakeInvoice("EUER-0001", InvoiceStatus.PartiallyPaid);
        invoice.Payments.Add(new InvoicePayment { Amount = 500m, PaymentDate = Utc(2025, 5, 1) });
        invoice.Payments.Add(new InvoicePayment { Amount = 300m, PaymentDate = Utc(2025, 5, 31) });
        invoice.Payments.Add(new InvoicePayment { Amount = 390m, PaymentDate = Utc(2025, 6, 1) });
        await SeedAsync(invoice);

        var result = (await _client.GetFromJsonAsync<List<InvoiceEuerPaymentDto>>(
            "/api/Invoice/euer-payments?from=2025-05-01&to=2025-05-31"))!;

        result.Should().HaveCount(2);
        result.Select(p => p.PaymentAmount).Should().Equal(500m, 300m);
        result.Should().AllSatisfy(p =>
        {
            p.InvoiceNumber.Should().Be("EUER-0001");
            p.InvoiceType.Should().Be("Invoice");
            p.InvoiceTotalGross.Should().Be(1190m);
            p.CustomerName.Should().Be("Nordlicht Media");
            p.Items.Should().ContainSingle(i => i.TotalNet == 1000m && i.TotalVat == 190m && i.TotalGross == 1190m);
        });
        result[0].PaymentDate.Should().Be(new DateOnly(2025, 5, 1));
    }

    [Fact]
    public async Task EuerPayments_GutschriftLiefertNegativeZahlungMitTyp()
    {
        var creditNote = MakeInvoice("EUER-GS-0001", InvoiceStatus.Paid, type: InvoiceType.CreditNote, unitPrice: -200m);
        creditNote.Payments.Add(new InvoicePayment { Amount = -238m, PaymentDate = Utc(2025, 7, 15) });
        await SeedAsync(creditNote);

        var result = await _client.GetFromJsonAsync<List<InvoiceEuerPaymentDto>>(
            "/api/Invoice/euer-payments?from=2025-07-01&to=2025-07-31");

        var payment = result.Should().ContainSingle().Subject;
        payment.InvoiceType.Should().Be("CreditNote");
        payment.PaymentAmount.Should().Be(-238m);
        payment.InvoiceTotalGross.Should().Be(-238m);
    }

    [Fact]
    public async Task EuerPayments_KeineZahlungenImZeitraum_LeereListe()
    {
        var result = await _client.GetFromJsonAsync<List<InvoiceEuerPaymentDto>>(
            "/api/Invoice/euer-payments?from=2020-01-01&to=2020-12-31");

        result.Should().BeEmpty();
    }

    // ─── Projektsummen (Acta) ─────────────────────────────────────────────────

    [Fact]
    public async Task ByProject_SummiertNettoBruttoUndBezahlteAnteile()
    {
        var paid = MakeInvoice("PRJ-0001", InvoiceStatus.Paid, projectId: 4711);
        paid.Payments.Add(new InvoicePayment { Amount = 1190m, PaymentDate = Utc(2025, 2, 1) });
        var partial = MakeInvoice("PRJ-0002", InvoiceStatus.PartiallyPaid, projectId: 4711, unitPrice: 500m);
        partial.DiscountType = DiscountType.Percentage;
        partial.DiscountValue = 10;
        partial.Payments.Add(new InvoicePayment { Amount = 267.75m, PaymentDate = Utc(2025, 2, 1) });
        await SeedAsync(paid);
        await SeedAsync(partial);
        await SeedAsync(MakeInvoice("PRJ-0003", projectId: 4712));

        var result = await _client.GetFromJsonAsync<ProjectInvoicesResponseDto>("/api/Invoice/project/4711");

        result!.InvoiceCount.Should().Be(2);
        result.TotalNet.Should().Be(1450m);
        result.TotalGross.Should().Be(1725.5m);
        result.TotalPaidGross.Should().Be(1457.75m);
        result.TotalPaidNet.Should().Be(1225m);
    }

    // ─── GetAll-Filter ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_FilterNachStatusUndKunde()
    {
        await SeedAsync(MakeInvoice("FLT-0001", InvoiceStatus.Overdue, customerId: 501));
        await SeedAsync(MakeInvoice("FLT-0002", InvoiceStatus.Sent, customerId: 501));
        await SeedAsync(MakeInvoice("FLT-0003", InvoiceStatus.Overdue, customerId: 502));

        var result = await _client.GetFromJsonAsync<List<InvoiceDto>>("/api/Invoice?status=Overdue&customerId=501");

        result!.Select(i => i.InvoiceNumber).Should().Equal("FLT-0001");
    }

    [Fact]
    public async Task GetAll_FilterNachTyp()
    {
        await SeedAsync(MakeInvoice("TYP-GS-0001", customerId: 601, type: InvoiceType.CreditNote));
        await SeedAsync(MakeInvoice("TYP-0001", customerId: 601));

        var result = await _client.GetFromJsonAsync<List<InvoiceDto>>("/api/Invoice?type=CreditNote&customerId=601");

        result!.Select(i => i.InvoiceNumber).Should().Equal("TYP-GS-0001");
    }

    [Fact]
    public async Task GetAll_ZahlzeitraumFiltertBezahlteNachZahldatum()
    {
        var inRange = MakeInvoice("PAID-0001", InvoiceStatus.Paid, customerId: 701);
        inRange.PaidDate = Utc(2024, 3, 15);
        var outOfRange = MakeInvoice("PAID-0002", InvoiceStatus.Paid, customerId: 701);
        outOfRange.PaidDate = Utc(2024, 4, 15);
        await SeedAsync(inRange);
        await SeedAsync(outOfRange);

        var result = await _client.GetFromJsonAsync<List<InvoiceDto>>(
            "/api/Invoice?paidFrom=2024-03-01&paidTo=2024-03-31&customerId=701");

        result!.Select(i => i.InvoiceNumber).Should().Equal("PAID-0001");
    }

    [Fact]
    public async Task GetAll_StatusMitEinseitigemZahlzeitraum_FiltertZusaetzlichNachZahldatum()
    {
        var early = MakeInvoice("PAIDF-0001", InvoiceStatus.Paid, customerId: 801);
        early.PaidDate = Utc(2024, 1, 15);
        var late = MakeInvoice("PAIDF-0002", InvoiceStatus.Paid, customerId: 801);
        late.PaidDate = Utc(2024, 6, 15);
        await SeedAsync(early);
        await SeedAsync(late);

        var from = await _client.GetFromJsonAsync<List<InvoiceDto>>("/api/Invoice?status=Paid&paidFrom=2024-03-01&customerId=801");
        var to = await _client.GetFromJsonAsync<List<InvoiceDto>>("/api/Invoice?status=Paid&paidTo=2024-03-01&customerId=801");

        from!.Select(i => i.InvoiceNumber).Should().Equal("PAIDF-0002");
        to!.Select(i => i.InvoiceNumber).Should().Equal("PAIDF-0001");
    }

    [Fact]
    public async Task GetById_GutschriftLiefertNummerDerBezugsrechnung()
    {
        var source = await SeedAsync(MakeInvoice("REL-0001"));
        var creditNote = MakeInvoice("REL-GS-0001", type: InvoiceType.CreditNote, unitPrice: -1000m);
        creditNote.RelatedInvoiceId = source.Id;
        await SeedAsync(creditNote);

        var result = await _client.GetFromJsonAsync<InvoiceDto>($"/api/Invoice/{creditNote.Id}");

        result!.RelatedInvoiceNumber.Should().Be("REL-0001");
        result.Type.Should().Be("CreditNote");
    }

    // ─── Audit-Log ────────────────────────────────────────────────────────────

    [Fact]
    public async Task AuditLog_LiefertEintraegeDerRechnung()
    {
        var invoice = await SeedAsync(MakeInvoice("AUD-0001", InvoiceStatus.Draft));

        var result = await _client.GetFromJsonAsync<List<AuditLogEntryDto>>($"/api/Invoice/{invoice.Id}/audit-log");

        result.Should().Contain(e => e.Action == "Created");
    }

    [Fact]
    public async Task AuditLogVerify_UnveraenderteKette_IstGueltig()
    {
        await SeedAsync(MakeInvoice("AUD-0002"));

        var result = await _client.GetFromJsonAsync<AuditChainVerificationDto>("/api/Invoice/audit-log/verify");

        result!.IsValid.Should().BeTrue();
        result.BrokenAtSequenceNumber.Should().BeNull();
    }

    [Fact]
    public async Task AuditLogVerify_RolleBuero_IstVerboten()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.Create("Buero"));

        var response = await client.GetAsync("/api/Invoice/audit-log/verify");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─── Versand / PDF ────────────────────────────────────────────────────────

    [Fact]
    public async Task Send_OhneEmpfaengerUndOhneKundenEmail_GibtBadRequest()
    {
        var invoice = await SeedAsync(MakeInvoice("SND-0001"));

        var response = await _client.PostAsJsonAsync($"/api/Invoice/{invoice.Id}/send", new SendInvoiceRequest());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Send_UnbekannteRechnung_GibtNotFound()
    {
        var response = await _client.PostAsJsonAsync("/api/Invoice/999999/send", new SendInvoiceRequest("a@b.de"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Pdf_LiefertPdfMitRechnungsnummerImDateinamen()
    {
        var invoice = await SeedAsync(MakeInvoice("PDF-0001"));

        var response = await _client.GetAsync($"/api/Invoice/{invoice.Id}/pdf");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        response.Content.Headers.ContentDisposition!.FileName.Should().Contain("Invoice-PDF-0001.pdf");
        (await response.Content.ReadAsByteArrayAsync()).Take(4).Should().Equal("%PDF"u8.ToArray());
    }

    [Fact]
    public async Task PdfPrint_LiefertHtmlMitEingebettetemPdfUndDruckaufruf()
    {
        var invoice = await SeedAsync(MakeInvoice("PRT-0001"));

        var response = await _client.GetAsync($"/api/Invoice/{invoice.Id}/pdf-print");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("<title>PRT-0001</title>");
        html.Should().Contain("data:application/pdf;base64,");
        html.Should().Contain("window.print()");
    }

    [Fact]
    public async Task PdfPrint_UnbekannteRechnung_GibtNotFound()
    {
        var response = await _client.GetAsync("/api/Invoice/999999/pdf-print");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
