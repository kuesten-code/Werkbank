using FluentAssertions;
using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.Contracts.Faktura;
using Kuestencode.Shared.Contracts.Recepta;
using Kuestencode.Werkbank.Saldo.Data.Repositories;
using Kuestencode.Werkbank.Saldo.Domain.Dtos;
using Kuestencode.Werkbank.Saldo.Domain.Entities;
using Kuestencode.Werkbank.Saldo.Domain.Enums;
using Kuestencode.Werkbank.Saldo.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Kuestencode.Werkbank.Saldo.Tests.Services;

public class EuerServiceTests
{
    private readonly Mock<IReceptaDataService> _receptaData = new();
    private readonly Mock<IFakturaApiClient> _fakturaClient = new();
    private readonly Mock<IKategorieKontoMappingRepository> _mappingRepo = new();
    private readonly Mock<IKontoRepository> _kontoRepo = new();
    private readonly Mock<ISaldoSettingsRepository> _settingsRepo = new();

    private static readonly DateOnly Von = new(2026, 1, 1);
    private static readonly DateOnly Bis = new(2026, 12, 31);

    private EuerService CreateService() =>
        new(_receptaData.Object, _fakturaClient.Object, _mappingRepo.Object,
            _kontoRepo.Object, _settingsRepo.Object, NullLogger<EuerService>.Instance);

    private void SetupKontenrahmen(string kontenrahmen = "SKR03")
    {
        _settingsRepo.Setup(r => r.GetAsync())
            .ReturnsAsync(new SaldoSettings { Kontenrahmen = kontenrahmen });
    }

    private static InvoiceDto CreateInvoice(
        int id, string invoiceNumber, string customerName,
        DateTime invoiceDate, DateTime paidDate,
        decimal netAmount, decimal vatRate, decimal vatAmount)
    {
        var gross = netAmount + vatAmount;
        return new InvoiceDto
        {
            Id = id,
            InvoiceNumber = invoiceNumber,
            CustomerName = customerName,
            InvoiceDate = invoiceDate,
            PaidDate = paidDate,
            Status = "Paid",
            Items = new List<InvoiceItemDto>
            {
                new()
                {
                    VatRate = vatRate,
                    TotalNet = netAmount,
                    TotalVat = vatAmount,
                    TotalGross = gross
                }
            },
            TotalNetAfterDiscount = netAmount,
            TotalVat = vatAmount,
            TotalGross = gross
        };
    }

    private static ReceptaDocumentDto CreateDocument(
        Guid id, string category, DateOnly invoiceDate, DateOnly? paidDate,
        decimal amountNet, decimal taxRate, decimal amountTax)
    {
        return new ReceptaDocumentDto
        {
            Id = id,
            Category = category,
            InvoiceDate = invoiceDate,
            PaidDate = paidDate,
            Status = "Paid",
            SupplierName = "Lieferant GmbH",
            AmountNet = amountNet,
            TaxRate = taxRate,
            AmountTax = amountTax,
            AmountGross = amountNet + amountTax
        };
    }

    // ─── Zufluss-/Abflussprinzip ──────────────────────────────────────────────

    [Fact]
    public async Task GetEuerSummary_ZaehltRechnungenNachBezahlDatumNichtRechnungsDatum()
    {
        // Rechnung vom Dezember 2025, aber bezahlt im Januar 2026
        // → muss im EÜR 2026 auftauchen
        SetupKontenrahmen();
        _kontoRepo.Setup(r => r.GetByKontenrahmenAsync("SKR03")).ReturnsAsync(new List<Konto>());
        _mappingRepo.Setup(r => r.GetAllAsync("SKR03")).ReturnsAsync(new List<KategorieKontoMapping>());
        _receptaData.Setup(s => s.GetDocumentsAsync(Von, Bis)).ReturnsAsync(new List<ReceptaDocumentDto>());

        // Faktura gibt bereits vorgefiltertes Ergebnis zurück (Status=Paid, PaidFrom/PaidTo)
        // → im EuerService wird einfach alles aggregiert was zurückkommt
        _fakturaClient.Setup(c => c.GetAllInvoicesAsync(It.IsAny<InvoiceFilterDto>()))
            .ReturnsAsync(new List<InvoiceDto>
            {
                CreateInvoice(1, "RE-2025-099", "Mustermann",
                    invoiceDate: new DateTime(2025, 12, 20),
                    paidDate:    new DateTime(2026, 1, 15),   // Zahlung im Jan 2026
                    netAmount: 1000m, vatRate: 19m, vatAmount: 190m)
            });

        var service = CreateService();
        var result = await service.GetEuerSummaryAsync(new EuerFilterDto { Von = Von, Bis = Bis });

        // Einnahmen müssen 1000 € betragen (Rechnungen nach PaidDate-Filter bereits vom API zurückgegeben)
        result.EinnahmenNetto.Should().Be(1000m);
        result.EinnahmenMwst.Should().Be(190m);
    }

    [Fact]
    public async Task GetEuerSummary_NurBezahlteRechnungenWerdenBerücksichtigt()
    {
        SetupKontenrahmen();
        _kontoRepo.Setup(r => r.GetByKontenrahmenAsync("SKR03")).ReturnsAsync(new List<Konto>());
        _mappingRepo.Setup(r => r.GetAllAsync("SKR03")).ReturnsAsync(new List<KategorieKontoMapping>());
        _receptaData.Setup(s => s.GetDocumentsAsync(Von, Bis)).ReturnsAsync(new List<ReceptaDocumentDto>());

        // Faktura filtert bereits nach Status=Paid → wir vertrauen dem Mock
        _fakturaClient.Setup(c => c.GetAllInvoicesAsync(It.Is<InvoiceFilterDto>(f => f.Status == "Paid")))
            .ReturnsAsync(new List<InvoiceDto>
            {
                CreateInvoice(1, "RE-001", "Kunde A",
                    new DateTime(2026, 1, 10), new DateTime(2026, 1, 15),
                    2000m, 19m, 380m)
            });

        var service = CreateService();
        var result = await service.GetEuerSummaryAsync(new EuerFilterDto { Von = Von, Bis = Bis });

        result.EinnahmenNetto.Should().Be(2000m);
        _fakturaClient.Verify(c => c.GetAllInvoicesAsync(
            It.Is<InvoiceFilterDto>(f => f.Status == "Paid")), Times.Once);
    }

    // ─── Aggregation nach Konten ──────────────────────────────────────────────

    [Fact]
    public async Task GetEuerSummary_GruppiertEinnahmenNachUstSatz()
    {
        SetupKontenrahmen();
        _mappingRepo.Setup(r => r.GetAllAsync("SKR03")).ReturnsAsync(new List<KategorieKontoMapping>());
        _receptaData.Setup(s => s.GetDocumentsAsync(Von, Bis)).ReturnsAsync(new List<ReceptaDocumentDto>());
        _kontoRepo.Setup(r => r.GetByKontenrahmenAsync("SKR03"))
            .ReturnsAsync(new List<Konto>
            {
                new() { KontoNummer = "8400", KontoBezeichnung = "Erlöse 19%", KontoTyp = KontoTyp.Einnahme, UstSatz = 19 },
                new() { KontoNummer = "8300", KontoBezeichnung = "Erlöse 7%",  KontoTyp = KontoTyp.Einnahme, UstSatz = 7  }
            });

        _fakturaClient.Setup(c => c.GetAllInvoicesAsync(It.IsAny<InvoiceFilterDto>()))
            .ReturnsAsync(new List<InvoiceDto>
            {
                new()
                {
                    Id = 1, InvoiceNumber = "RE-001", InvoiceDate = new DateTime(2026, 2, 1),
                    PaidDate = new DateTime(2026, 2, 1),
                    Items = new List<InvoiceItemDto>
                    {
                        new() { VatRate = 19m, TotalNet = 1000m, TotalVat = 190m, TotalGross = 1190m },
                        new() { VatRate = 7m,  TotalNet = 500m,  TotalVat = 35m,  TotalGross = 535m  }
                    }
                }
            });

        var service = CreateService();
        var result = await service.GetEuerSummaryAsync(new EuerFilterDto { Von = Von, Bis = Bis });

        result.Einnahmen.Should().HaveCount(2); // 2 USt-Gruppen
        result.Einnahmen.Sum(p => p.BetragNetto).Should().Be(1500m);

        var pos19 = result.Einnahmen.Single(p => p.KontoNummer == "8400");
        pos19.BetragNetto.Should().Be(1000m);
        pos19.MwstBetrag.Should().Be(190m);

        var pos7 = result.Einnahmen.Single(p => p.KontoNummer == "8300");
        pos7.BetragNetto.Should().Be(500m);
    }

    [Fact]
    public async Task GetEuerSummary_GruppiertAusgabenNachKategorie()
    {
        SetupKontenrahmen();
        _fakturaClient.Setup(c => c.GetAllInvoicesAsync(It.IsAny<InvoiceFilterDto>()))
            .ReturnsAsync(new List<InvoiceDto>());
        _kontoRepo.Setup(r => r.GetByKontenrahmenAsync("SKR03"))
            .ReturnsAsync(new List<Konto>
            {
                new() { KontoNummer = "4930", KontoBezeichnung = "Bürobedarf",   KontoTyp = KontoTyp.Ausgabe },
                new() { KontoNummer = "4660", KontoBezeichnung = "Reisekosten",  KontoTyp = KontoTyp.Ausgabe }
            });
        _mappingRepo.Setup(r => r.GetAllAsync("SKR03"))
            .ReturnsAsync(new List<KategorieKontoMapping>
            {
                new() { ReceiptaKategorie = "Büromaterial", KontoNummer = "4930" },
                new() { ReceiptaKategorie = "Reise",        KontoNummer = "4660" }
            });
        _receptaData.Setup(s => s.GetDocumentsAsync(Von, Bis))
            .ReturnsAsync(new List<ReceptaDocumentDto>
            {
                CreateDocument(Guid.NewGuid(), "Büromaterial", new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 5), 100m, 19m, 19m),
                CreateDocument(Guid.NewGuid(), "Büromaterial", new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 1), 200m, 19m, 38m),
                CreateDocument(Guid.NewGuid(), "Reise",        new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 1), 150m, 19m, 28.5m)
            });

        var service = CreateService();
        var result = await service.GetEuerSummaryAsync(new EuerFilterDto { Von = Von, Bis = Bis });

        result.Ausgaben.Should().HaveCount(2); // 2 Kategorien

        var buero = result.Ausgaben.Single(p => p.KontoNummer == "4930");
        buero.BetragNetto.Should().Be(300m); // 100 + 200
        buero.AnzahlBelege.Should().Be(2);

        var reise = result.Ausgaben.Single(p => p.KontoNummer == "4660");
        reise.BetragNetto.Should().Be(150m);
        reise.AnzahlBelege.Should().Be(1);
    }

    // ─── Edge Cases ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetEuerSummary_LeererZeitraum_GibtLeereListenZurueck()
    {
        SetupKontenrahmen();
        _kontoRepo.Setup(r => r.GetByKontenrahmenAsync("SKR03")).ReturnsAsync(new List<Konto>());
        _mappingRepo.Setup(r => r.GetAllAsync("SKR03")).ReturnsAsync(new List<KategorieKontoMapping>());
        _fakturaClient.Setup(c => c.GetAllInvoicesAsync(It.IsAny<InvoiceFilterDto>()))
            .ReturnsAsync(new List<InvoiceDto>());
        _receptaData.Setup(s => s.GetDocumentsAsync(Von, Bis))
            .ReturnsAsync(new List<ReceptaDocumentDto>());

        var service = CreateService();
        var result = await service.GetEuerSummaryAsync(new EuerFilterDto { Von = Von, Bis = Bis });

        result.EinnahmenNetto.Should().Be(0m);
        result.AusgabenNetto.Should().Be(0m);
        result.Ueberschuss.Should().Be(0m);
        result.Einnahmen.Should().BeEmpty();
        result.Ausgaben.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEuerSummary_FakturaFehlerGibtLeereListe()
    {
        // Wenn Faktura nicht erreichbar → graceful degradation
        SetupKontenrahmen();
        _kontoRepo.Setup(r => r.GetByKontenrahmenAsync("SKR03")).ReturnsAsync(new List<Konto>());
        _mappingRepo.Setup(r => r.GetAllAsync("SKR03")).ReturnsAsync(new List<KategorieKontoMapping>());
        _receptaData.Setup(s => s.GetDocumentsAsync(Von, Bis)).ReturnsAsync(new List<ReceptaDocumentDto>());

        _fakturaClient.Setup(c => c.GetAllInvoicesAsync(It.IsAny<InvoiceFilterDto>()))
            .ThrowsAsync(new HttpRequestException("Faktura nicht erreichbar"));

        var service = CreateService();

        // Sollte nicht werfen, sondern Einnahmen = 0 liefern
        var result = await service.GetEuerSummaryAsync(new EuerFilterDto { Von = Von, Bis = Bis });

        result.EinnahmenNetto.Should().Be(0m);
    }

    [Fact]
    public async Task GetEuerSummary_BelegOhneKategorieMappingNutztFallbackKonto()
    {
        SetupKontenrahmen();
        _fakturaClient.Setup(c => c.GetAllInvoicesAsync(It.IsAny<InvoiceFilterDto>()))
            .ReturnsAsync(new List<InvoiceDto>());
        _kontoRepo.Setup(r => r.GetByKontenrahmenAsync("SKR03")).ReturnsAsync(new List<Konto>());
        _mappingRepo.Setup(r => r.GetAllAsync("SKR03")).ReturnsAsync(new List<KategorieKontoMapping>()); // kein Mapping

        _receptaData.Setup(s => s.GetDocumentsAsync(Von, Bis))
            .ReturnsAsync(new List<ReceptaDocumentDto>
            {
                CreateDocument(Guid.NewGuid(), "UnbekanntKategorie",
                    new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1), 100m, 0m, 0m)
            });

        var service = CreateService();
        var result = await service.GetEuerSummaryAsync(new EuerFilterDto { Von = Von, Bis = Bis });

        result.Ausgaben.Should().HaveCount(1);
        result.Ausgaben[0].KontoNummer.Should().Be("4980"); // Fallback-Konto
    }

    // ─── Berechnung Überschuss ────────────────────────────────────────────────

    [Fact]
    public async Task GetEuerSummary_UeberschussBerechnetKorrekt()
    {
        SetupKontenrahmen();
        _kontoRepo.Setup(r => r.GetByKontenrahmenAsync("SKR03")).ReturnsAsync(new List<Konto>());
        _mappingRepo.Setup(r => r.GetAllAsync("SKR03")).ReturnsAsync(new List<KategorieKontoMapping>());

        _fakturaClient.Setup(c => c.GetAllInvoicesAsync(It.IsAny<InvoiceFilterDto>()))
            .ReturnsAsync(new List<InvoiceDto>
            {
                CreateInvoice(1, "RE-001", "Kunde", new DateTime(2026, 1, 1), new DateTime(2026, 1, 1), 5000m, 19m, 950m)
            });
        _receptaData.Setup(s => s.GetDocumentsAsync(Von, Bis))
            .ReturnsAsync(new List<ReceptaDocumentDto>
            {
                CreateDocument(Guid.NewGuid(), "Material", new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 1), 2000m, 19m, 380m)
            });

        var service = CreateService();
        var result = await service.GetEuerSummaryAsync(new EuerFilterDto { Von = Von, Bis = Bis });

        result.EinnahmenNetto.Should().Be(5000m);
        result.AusgabenNetto.Should().Be(2000m);
        result.Ueberschuss.Should().Be(3000m); // 5000 - 2000
    }
}
