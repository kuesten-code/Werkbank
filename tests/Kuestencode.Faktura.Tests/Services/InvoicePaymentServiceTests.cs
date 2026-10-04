using FluentAssertions;
using Kuestencode.Faktura.Data;
using Kuestencode.Faktura.Data.Repositories;
using Kuestencode.Faktura.Models;
using Kuestencode.Faktura.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Kuestencode.Faktura.Tests.Services;

public class InvoicePaymentServiceTests
{
    private readonly FakturaDbContext _context;
    private readonly InvoicePaymentService _service;

    public InvoicePaymentServiceTests()
    {
        var options = new DbContextOptionsBuilder<FakturaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new FakturaDbContext(options);
        _service = new InvoicePaymentService(
            new InvoicePaymentRepository(_context), _context, NullLogger<InvoicePaymentService>.Instance);
    }

    private async Task<Invoice> SeedInvoiceAsync(InvoiceStatus status, DateTime? dueDate)
    {
        var invoice = new Invoice
        {
            Id = 1,
            InvoiceNumber = "R-2026-0001",
            InvoiceDate = DateTime.UtcNow.AddDays(-30),
            CustomerId = 1,
            Status = status,
            DueDate = dueDate,
            Items = { new InvoiceItem { Quantity = 1, UnitPrice = 100m, VatRate = 19m } }
        };
        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();
        return invoice;
    }

    [Fact]
    public async Task ZahlungErfassen_TeilzahlungNachFaelligkeit_SetztOverdue()
    {
        await SeedInvoiceAsync(InvoiceStatus.Overdue, DateTime.UtcNow.AddDays(-1));

        await _service.ZahlungErfassenAsync(1, 50m, DateTime.Today, null);

        (await _context.Invoices.FindAsync(1))!.Status.Should().Be(InvoiceStatus.Overdue);
    }

    [Fact]
    public async Task ZahlungszielSetzen_SpeichertNeuesZielOhneFaelligkeitsdatumZuAendern()
    {
        var originalDueDate = DateTime.UtcNow.AddDays(-1);
        await SeedInvoiceAsync(InvoiceStatus.Overdue, originalDueDate);
        await _service.ZahlungErfassenAsync(1, 50m, DateTime.Today, null);

        var newTarget = DateTime.Today.AddDays(14);
        await _service.ZahlungszielSetzenAsync(1, newTarget);

        var invoice = (await _context.Invoices.FindAsync(1))!;
        invoice.DueDate.Should().Be(originalDueDate);
        invoice.RevisedDueDate.Should().Be(DateTime.SpecifyKind(newTarget, DateTimeKind.Utc));
        invoice.RevisedDueDate!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task ZahlungszielSetzen_InZukunft_SetztUeberfaelligeTeilzahlungAufPartiallyPaid()
    {
        await SeedInvoiceAsync(InvoiceStatus.Overdue, DateTime.UtcNow.AddDays(-1));
        await _service.ZahlungErfassenAsync(1, 50m, DateTime.Today, null);

        await _service.ZahlungszielSetzenAsync(1, DateTime.Today.AddDays(14));

        (await _context.Invoices.FindAsync(1))!.Status.Should().Be(InvoiceStatus.PartiallyPaid);
    }

    [Fact]
    public async Task ZahlungszielSetzen_UnbekannteRechnung_WirftException()
    {
        var act = () => _service.ZahlungszielSetzenAsync(999, DateTime.Today);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ZahlungErfassen_VollerBetrag_SetztPaidUndZahldatumAufLetzteZahlung()
    {
        await SeedInvoiceAsync(InvoiceStatus.Sent, DateTime.UtcNow.AddDays(14));

        await _service.ZahlungErfassenAsync(1, 19m, new DateTime(2026, 3, 1), null);
        await _service.ZahlungErfassenAsync(1, 100m, new DateTime(2026, 3, 10, 15, 30, 0), "Rest");

        var invoice = (await _context.Invoices.FindAsync(1))!;
        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.PaidDate.Should().Be(new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task ZahlungErfassen_Ueberzahlung_GiltAlsBezahlt()
    {
        await SeedInvoiceAsync(InvoiceStatus.Sent, DateTime.UtcNow.AddDays(14));

        await _service.ZahlungErfassenAsync(1, 150m, DateTime.Today, null);

        (await _context.Invoices.FindAsync(1))!.Status.Should().Be(InvoiceStatus.Paid);
    }

    [Fact]
    public async Task ZahlungErfassen_TeilzahlungVorFaelligkeit_SetztPartiallyPaidOhneZahldatum()
    {
        await SeedInvoiceAsync(InvoiceStatus.Sent, DateTime.UtcNow.AddDays(14));

        await _service.ZahlungErfassenAsync(1, 50m, DateTime.Today, null);

        var invoice = (await _context.Invoices.FindAsync(1))!;
        invoice.Status.Should().Be(InvoiceStatus.PartiallyPaid);
        invoice.PaidDate.Should().BeNull();
    }

    [Theory]
    [InlineData(InvoiceStatus.Draft)]
    [InlineData(InvoiceStatus.Cancelled)]
    public async Task ZahlungErfassen_EntwurfOderStorniert_StatusBleibtUnveraendert(InvoiceStatus status)
    {
        await SeedInvoiceAsync(status, DateTime.UtcNow.AddDays(14));

        await _service.ZahlungErfassenAsync(1, 119m, DateTime.Today, null);

        (await _context.Invoices.FindAsync(1))!.Status.Should().Be(status);
    }

    [Fact]
    public async Task ZahlungErfassen_GutschriftNegativeZahlung_SetztPaid()
    {
        _context.Invoices.Add(new Invoice
        {
            Id = 2, InvoiceNumber = "GS-2026-0001", InvoiceDate = DateTime.UtcNow, CustomerId = 1,
            Type = InvoiceType.CreditNote, Status = InvoiceStatus.Sent,
            Items = { new InvoiceItem { Quantity = 1, UnitPrice = -100m, VatRate = 19m } }
        });
        await _context.SaveChangesAsync();

        await _service.ZahlungErfassenAsync(2, -119m, DateTime.Today, null);

        (await _context.Invoices.FindAsync(2))!.Status.Should().Be(InvoiceStatus.Paid);
    }

    [Fact]
    public async Task ZahlungLoeschen_LetzteZahlungEinerBezahltenRechnung_SetztZurueckAufSentOhneZahldatum()
    {
        await SeedInvoiceAsync(InvoiceStatus.Sent, DateTime.UtcNow.AddDays(14));
        await _service.ZahlungErfassenAsync(1, 119m, DateTime.Today, null);
        var payment = (await _service.GetZahlungenAsync(1)).Single();

        await _service.ZahlungLoeschenAsync(payment.Id);

        var invoice = (await _context.Invoices.FindAsync(1))!;
        invoice.Status.Should().Be(InvoiceStatus.Sent);
        invoice.PaidDate.Should().BeNull();
        (await _service.GetZahlungenAsync(1)).Should().BeEmpty();
    }

    [Fact]
    public async Task ZahlungLoeschen_EineVonZweiZahlungen_SetztPartiallyPaid()
    {
        await SeedInvoiceAsync(InvoiceStatus.Sent, DateTime.UtcNow.AddDays(14));
        await _service.ZahlungErfassenAsync(1, 60m, DateTime.Today.AddDays(-1), null);
        await _service.ZahlungErfassenAsync(1, 59m, DateTime.Today, null);
        var latest = (await _service.GetZahlungenAsync(1)).First();

        await _service.ZahlungLoeschenAsync(latest.Id);

        (await _context.Invoices.FindAsync(1))!.Status.Should().Be(InvoiceStatus.PartiallyPaid);
    }

    [Fact]
    public async Task ZahlungLoeschen_UnbekannteZahlung_WirftException()
    {
        var act = () => _service.ZahlungLoeschenAsync(999);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetByPaymentDateRange_LiefertZahlungenImZeitraum()
    {
        await SeedInvoiceAsync(InvoiceStatus.Sent, DateTime.UtcNow.AddDays(14));
        await _service.ZahlungErfassenAsync(1, 10m, new DateTime(2026, 3, 5), null);
        await _service.ZahlungErfassenAsync(1, 20m, new DateTime(2026, 4, 5), null);

        var result = await _service.GetByPaymentDateRangeAsync(
            new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc));

        result.Select(p => p.Amount).Should().Equal(10m);
    }

    [Fact]
    public async Task ZahlungErfassen_SchlussrechnungMitAbschlag_ZuZahlenBeglichen_SetztPaid()
    {
        _context.Invoices.Add(new Invoice
        {
            Id = 3, InvoiceNumber = "R-2026-0003", InvoiceDate = DateTime.UtcNow, CustomerId = 1,
            Status = InvoiceStatus.Sent, DueDate = DateTime.UtcNow.AddDays(14),
            Items = { new InvoiceItem { Quantity = 1, UnitPrice = 1000m, VatRate = 19m } },
            DownPayments = { new DownPayment { Description = "AR-1", Amount = 595m } }
        });
        await _context.SaveChangesAsync();

        await _service.ZahlungErfassenAsync(3, 595m, DateTime.Today, null);

        (await _context.Invoices.FindAsync(3))!.Status.Should().Be(InvoiceStatus.Paid);
    }

    [Fact]
    public async Task ZahlungErfassen_SchlussrechnungMitAbschlag_TeilDesRests_SetztPartiallyPaid()
    {
        _context.Invoices.Add(new Invoice
        {
            Id = 4, InvoiceNumber = "R-2026-0004", InvoiceDate = DateTime.UtcNow, CustomerId = 1,
            Status = InvoiceStatus.Sent, DueDate = DateTime.UtcNow.AddDays(14),
            Items = { new InvoiceItem { Quantity = 1, UnitPrice = 1000m, VatRate = 19m } },
            DownPayments = { new DownPayment { Description = "AR-1", Amount = 595m } }
        });
        await _context.SaveChangesAsync();

        await _service.ZahlungErfassenAsync(4, 300m, DateTime.Today, null);

        (await _context.Invoices.FindAsync(4))!.Status.Should().Be(InvoiceStatus.PartiallyPaid);
    }
}
