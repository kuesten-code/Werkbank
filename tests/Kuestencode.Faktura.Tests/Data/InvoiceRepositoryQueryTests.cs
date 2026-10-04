using FluentAssertions;
using Kuestencode.Faktura.Data;
using Kuestencode.Faktura.Data.Repositories;
using Kuestencode.Faktura.Models;
using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.Contracts.Host;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Kuestencode.Faktura.Tests.Data;

public class InvoiceRepositoryQueryTests
{
    private readonly FakturaDbContext _context;
    private readonly Mock<IHostApiClient> _hostApiClient = new();
    private readonly InvoiceRepository _repository;

    public InvoiceRepositoryQueryTests()
    {
        var options = new DbContextOptionsBuilder<FakturaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new FakturaDbContext(options);

        _hostApiClient.Setup(c => c.GetAllCustomersAsync())
            .ReturnsAsync([new CustomerDto { Id = 1, Name = "Hafen GmbH" }, new CustomerDto { Id = 2, Name = "Deich KG" }]);
        _hostApiClient.Setup(c => c.GetCustomerAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => new CustomerDto { Id = id, Name = $"Kunde {id}", Email = "k@example.de" });

        _repository = new InvoiceRepository(_context, _hostApiClient.Object);
    }

    private static DateTime Utc(int year, int month, int day) => new(year, month, day, 0, 0, 0, DateTimeKind.Utc);

    private static Invoice MakeInvoice(int id, InvoiceStatus status = InvoiceStatus.Sent, InvoiceType type = InvoiceType.Invoice,
        DateTime? invoiceDate = null, DateTime? paidDate = null, int customerId = 1, int? projectId = null) =>
        new()
        {
            Id = id,
            InvoiceNumber = $"R-{id:D4}",
            InvoiceDate = invoiceDate ?? Utc(2026, 3, 1),
            CustomerId = customerId,
            ProjectId = projectId,
            Status = status,
            Type = type,
            PaidDate = paidDate
        };

    private async Task SeedAsync(params Invoice[] invoices)
    {
        _context.Invoices.AddRange(invoices);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }

    // ─── GetPaidByDateRangeAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetPaidByDateRange_NurBezahlteImZeitraumInklusiveGrenzen_AufsteigendNachZahldatum()
    {
        await SeedAsync(
            MakeInvoice(1, InvoiceStatus.Paid, paidDate: Utc(2026, 3, 31)),
            MakeInvoice(2, InvoiceStatus.Paid, paidDate: Utc(2026, 3, 1)),
            MakeInvoice(3, InvoiceStatus.Paid, paidDate: Utc(2026, 2, 28)),
            MakeInvoice(4, InvoiceStatus.Paid, paidDate: Utc(2026, 4, 1)),
            MakeInvoice(5, InvoiceStatus.PartiallyPaid, paidDate: Utc(2026, 3, 10)),
            MakeInvoice(6, InvoiceStatus.Paid));

        var result = await _repository.GetPaidByDateRangeAsync(new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));

        result.Select(i => i.Id).Should().Equal(2, 1);
    }

    // ─── GetByTypeAsync / GetByStatusAsync / GetByProjectIdAsync ──────────────

    [Fact]
    public async Task GetByType_FiltertTypUndSortiertNeuesteZuerst()
    {
        await SeedAsync(
            MakeInvoice(1, type: InvoiceType.CreditNote, invoiceDate: Utc(2026, 1, 1)),
            MakeInvoice(2, type: InvoiceType.CreditNote, invoiceDate: Utc(2026, 2, 1)),
            MakeInvoice(3));

        var result = await _repository.GetByTypeAsync(InvoiceType.CreditNote);

        result.Select(i => i.Id).Should().Equal(2, 1);
    }

    [Fact]
    public async Task GetByStatus_FiltertStatusUndLaedtKunden()
    {
        await SeedAsync(
            MakeInvoice(1, InvoiceStatus.Overdue, customerId: 2),
            MakeInvoice(2, InvoiceStatus.Sent));

        var result = (await _repository.GetByStatusAsync(InvoiceStatus.Overdue)).ToList();

        result.Should().ContainSingle().Which.Customer!.Name.Should().Be("Deich KG");
    }

    [Fact]
    public async Task GetByProjectId_NurRechnungenDesProjekts()
    {
        await SeedAsync(
            MakeInvoice(1, projectId: 7),
            MakeInvoice(2, projectId: 8),
            MakeInvoice(3));

        var result = await _repository.GetByProjectIdAsync(7);

        result.Select(i => i.Id).Should().Equal(1);
    }

    [Fact]
    public async Task GetAll_LaedtPositionenZahlungenUndKunden_UnbekannterKundeBleibtLeer()
    {
        var invoice = MakeInvoice(1, invoiceDate: Utc(2026, 1, 1));
        invoice.Items.Add(new InvoiceItem { Description = "A", Quantity = 1, UnitPrice = 100, VatRate = 19 });
        invoice.Payments.Add(new InvoicePayment { Amount = 50, PaymentDate = Utc(2026, 1, 5) });
        await SeedAsync(invoice, MakeInvoice(2, invoiceDate: Utc(2026, 2, 1), customerId: 99));

        var result = (await _repository.GetAllAsync()).ToList();

        result.Select(i => i.Id).Should().Equal(2, 1);
        result[1].Items.Should().ContainSingle();
        result[1].Payments.Should().ContainSingle();
        result[1].Customer!.Name.Should().Be("Hafen GmbH");
        result[0].Customer.Should().BeNull();
    }

    // ─── GetWithDetailsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetWithDetails_PositionenNachPositionSortiertUndKundeGeladen()
    {
        var invoice = MakeInvoice(1);
        invoice.Items.Add(new InvoiceItem { Description = "Zwei", Position = 2, Quantity = 1, UnitPrice = 1 });
        invoice.Items.Add(new InvoiceItem { Description = "Eins", Position = 1, Quantity = 1, UnitPrice = 1 });
        await SeedAsync(invoice);

        var result = await _repository.GetWithDetailsAsync(1);

        result!.Items.Select(i => i.Description).Should().Equal("Eins", "Zwei");
        result.Customer!.Name.Should().Be("Kunde 1");
    }

    [Fact]
    public async Task GetWithDetails_AbschlagMitQuellrechnung_LaedtQuellrechnungMitPositionenUndZahlungen()
    {
        var source = MakeInvoice(1);
        source.Items.Add(new InvoiceItem { Description = "Abschlag", Quantity = 1, UnitPrice = 1000, VatRate = 19 });
        source.Payments.Add(new InvoicePayment { Amount = 1190, PaymentDate = Utc(2026, 3, 5) });
        var final = MakeInvoice(2);
        final.DownPayments.Add(new DownPayment { Description = "AR R-0001", Amount = 1190, SourceInvoiceId = 1 });
        final.DownPayments.Add(new DownPayment { Description = "manuell", Amount = 100 });
        await SeedAsync(source, final);

        var result = await _repository.GetWithDetailsAsync(2);

        var linked = result!.DownPayments.Single(d => d.SourceInvoiceId == 1);
        linked.SourceInvoice!.InvoiceNumber.Should().Be("R-0001");
        linked.SourceInvoice.TotalGross.Should().Be(1190m);
        linked.SourceInvoice.TotalPaid.Should().Be(1190m);
        result.DownPayments.Single(d => d.SourceInvoiceId == null).SourceInvoice.Should().BeNull();
    }

    [Fact]
    public async Task GetById_UnbekannteId_LiefertNull()
    {
        (await _repository.GetByIdAsync(404)).Should().BeNull();
        _hostApiClient.Verify(c => c.GetCustomerAsync(It.IsAny<int>()), Times.Never);
    }

    // ─── Nummernvergabe ───────────────────────────────────────────────────────

    [Fact]
    public async Task InvoiceNumberExists_ErkenntVorhandeneNummer()
    {
        await SeedAsync(MakeInvoice(1));

        (await _repository.InvoiceNumberExistsAsync("R-0001")).Should().BeTrue();
        (await _repository.InvoiceNumberExistsAsync("R-0002")).Should().BeFalse();
    }

    [Fact]
    public async Task GenerateInvoiceNumber_OhneHostEinstellung_StandardformatUndFortlaufend()
    {
        var year = DateTime.Now.Year;
        var existing = MakeInvoice(1);
        existing.InvoiceNumber = $"{year}-0007";
        await SeedAsync(existing);

        var number = await _repository.GenerateInvoiceNumberAsync();

        number.Should().Be($"{year}-0008");
    }

    [Fact]
    public async Task GenerateInvoiceNumber_EigenesFormat_WirdGetrimmtVerwendet()
    {
        _hostApiClient.Setup(c => c.GetNumberFormatSettingsAsync())
            .ReturnsAsync(new NumberFormatSettingsDto { InvoiceFormat = "  RE-YY-XXX  " });

        var number = await _repository.GenerateInvoiceNumberAsync();

        number.Should().Be($"RE-{DateTime.Now:yy}-001");
    }

    [Fact]
    public async Task GenerateCreditNoteNumber_EigenerNummernkreisUnabhaengigVonRechnungen()
    {
        var year = DateTime.Now.Year;
        var invoice = MakeInvoice(1);
        invoice.InvoiceNumber = $"GS-{year}-0005";
        var creditNote = MakeInvoice(2, type: InvoiceType.CreditNote);
        creditNote.InvoiceNumber = $"GS-{year}-0002";
        await SeedAsync(invoice, creditNote);

        var number = await _repository.GenerateCreditNoteNumberAsync();

        number.Should().Be($"GS-{year}-0003");
    }

    [Fact]
    public async Task NummernformatTeile_LiefernPraefixSuffixUndBreite()
    {
        _hostApiClient.Setup(c => c.GetNumberFormatSettingsAsync())
            .ReturnsAsync(new NumberFormatSettingsDto { InvoiceFormat = "YYYY-XXXX-RE", CreditNoteFormat = " " });

        var invoiceParts = await _repository.GetInvoiceNumberFormatPartsAsync();
        var creditNoteParts = await _repository.GetCreditNoteNumberFormatPartsAsync();

        invoiceParts.Should().Be(($"{DateTime.Now.Year}-", "-RE", 4));
        creditNoteParts.Should().Be(($"GS-{DateTime.Now.Year}-", "", 4));
    }
}
