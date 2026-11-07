using FluentAssertions;
using Kuestencode.Faktura.Data.Repositories;
using Kuestencode.Faktura.Models;
using Kuestencode.Faktura.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Kuestencode.Faktura.Tests.Services;

public class LegacyInvoiceImportServiceTests
{
    private static readonly byte[] Pdf = [0x25, 0x50, 0x44, 0x46];

    private readonly Mock<IInvoiceRepository> _repo = new();
    private readonly Mock<IInvoicePaymentService> _paymentService = new();
    private readonly LegacyInvoiceImportService _service;
    private Invoice? _added;

    public LegacyInvoiceImportServiceTests()
    {
        _repo.Setup(r => r.AddAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>(i => { i.Id = 42; _added = i; })
            .ReturnsAsync((Invoice i) => i);
        _service = new LegacyInvoiceImportService(_repo.Object, _paymentService.Object, NullLogger<LegacyInvoiceImportService>.Instance);
    }

    private static LegacyInvoiceImport MakeImport(
        string number = " RE-2025-0815 ",
        int customerId = 7,
        DateTime? dueDate = null,
        decimal net = 1000m,
        decimal vatRate = 19m,
        decimal alreadyPaid = 0m,
        DateTime? alreadyPaidDate = null,
        byte[]? pdf = null,
        string? notes = null) =>
        new(number, customerId, new DateTime(2025, 11, 3), dueDate ?? DateTime.Today.AddDays(10),
            net, vatRate, alreadyPaid, alreadyPaidDate, "RE-2025-0815.pdf", pdf ?? Pdf, notes);

    [Fact]
    public async Task Import_LegtAltrechnungMitOriginalPdfAlsEingefrorenemBelegAn()
    {
        var invoice = await _service.ImportAsync(MakeImport(notes: "  Mahnung folgt  "));

        invoice.Should().BeSameAs(_added);
        invoice.InvoiceNumber.Should().Be("RE-2025-0815");
        invoice.IsImported.Should().BeTrue();
        invoice.Type.Should().Be(InvoiceType.Invoice);
        invoice.CustomerId.Should().Be(7);
        invoice.Notes.Should().Be("Mahnung folgt");
        invoice.InvoiceDate.Should().Be(new DateTime(2025, 11, 3));
        invoice.InvoiceDate.Kind.Should().Be(DateTimeKind.Utc);
        invoice.DueDate!.Value.Kind.Should().Be(DateTimeKind.Utc);
        invoice.Items.Should().ContainSingle();
        invoice.TotalGross.Should().Be(1190m);

        var attachment = invoice.Attachments.Should().ContainSingle().Subject;
        attachment.IsFrozenSnapshot.Should().BeTrue();
        attachment.ContentType.Should().Be("application/pdf");
        attachment.FileName.Should().Be("RE-2025-0815.pdf");
        attachment.Data.Should().Equal(Pdf);
        attachment.FileSize.Should().Be(Pdf.Length);
    }

    [Fact]
    public async Task Import_FaelligkeitInZukunft_StatusVersendet()
    {
        var invoice = await _service.ImportAsync(MakeImport());

        invoice.Status.Should().Be(InvoiceStatus.Sent);
    }

    [Fact]
    public async Task Import_FaelligkeitVergangen_StatusUeberfaellig()
    {
        var invoice = await _service.ImportAsync(MakeImport(dueDate: new DateTime(2025, 11, 17)));

        invoice.Status.Should().Be(InvoiceStatus.Overdue);
    }

    [Fact]
    public async Task Import_LeereNotiz_WirdNichtGespeichert()
    {
        var invoice = await _service.ImportAsync(MakeImport(notes: "   "));

        invoice.Notes.Should().BeNull();
    }

    [Fact]
    public async Task Import_OhneTeilzahlung_ErfasstKeineZahlung()
    {
        await _service.ImportAsync(MakeImport());

        _paymentService.Verify(p => p.ZahlungErfassenAsync(It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<DateTime>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task Import_MitTeilzahlung_ErfasstZahlungNachDemSpeichern()
    {
        var paidDate = new DateTime(2025, 12, 1);

        await _service.ImportAsync(MakeImport(alreadyPaid: 500m, alreadyPaidDate: paidDate));

        _paymentService.Verify(p => p.ZahlungErfassenAsync(42, 500m, paidDate, "Teilzahlung laut Vorsystem"), Times.Once);
    }

    [Fact]
    public async Task Import_RechnungsnummerExistiert_WirftUndSpeichertNicht()
    {
        _repo.Setup(r => r.InvoiceNumberExistsAsync("RE-2025-0815")).ReturnsAsync(true);

        var act = () => _service.ImportAsync(MakeImport());

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*existiert bereits*");
        _repo.Verify(r => r.AddAsync(It.IsAny<Invoice>()), Times.Never);
    }

    public static TheoryData<LegacyInvoiceImport, string> UngueltigeEingaben => new()
    {
        { MakeImport(number: "  "), "*Rechnungsnummer ist erforderlich*" },
        { MakeImport(number: new string('1', 21)), "*höchstens 20 Zeichen*" },
        { MakeImport(customerId: 0), "*Kunde*" },
        { MakeImport(net: 0m), "*Nettobetrag*" },
        { MakeImport(vatRate: 16m), "*MwSt-Satz*" },
        { MakeImport(alreadyPaid: -1m, alreadyPaidDate: DateTime.Today), "*negativ*" },
        { MakeImport(alreadyPaid: 1190m, alreadyPaidDate: DateTime.Today), "*Vollständig bezahlte*" },
        { MakeImport(alreadyPaid: 100m), "*Zahlungsdatum*" },
        { MakeImport(pdf: []), "*Original-PDF*" }
    };

    [Theory]
    [MemberData(nameof(UngueltigeEingaben))]
    public async Task Import_UngueltigeEingabe_WirftArgumentException(LegacyInvoiceImport import, string expectedMessage)
    {
        var act = () => _service.ImportAsync(import);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage(expectedMessage);
        _repo.Verify(r => r.AddAsync(It.IsAny<Invoice>()), Times.Never);
    }

    [Fact]
    public async Task Import_ZwanzigZeichenNummer_IstErlaubt()
    {
        var invoice = await _service.ImportAsync(MakeImport(number: new string('1', 20)));

        invoice.InvoiceNumber.Should().HaveLength(20);
    }
}
