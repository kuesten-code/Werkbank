using FluentAssertions;
using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.Contracts.Faktura;
using Kuestencode.Werkbank.Saldo.Data.Repositories;
using Kuestencode.Werkbank.Saldo.Domain.Dtos;
using Kuestencode.Werkbank.Saldo.Domain.Entities;
using Kuestencode.Werkbank.Saldo.Domain.Enums;
using Kuestencode.Werkbank.Saldo.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Kuestencode.Werkbank.Saldo.Tests.Services;

public class EinnahmenServiceTests
{
    private readonly Mock<IFakturaApiClient> _fakturaClient = new();
    private readonly Mock<IKontoRepository> _kontoRepo = new();
    private readonly Mock<ISaldoSettingsRepository> _settingsRepo = new();

    private static readonly DateOnly Von = new(2026, 1, 1);
    private static readonly DateOnly Bis = new(2026, 12, 31);

    private EinnahmenService CreateService() =>
        new(_fakturaClient.Object, _kontoRepo.Object, _settingsRepo.Object,
            NullLogger<EinnahmenService>.Instance);

    private void SetupKontenrahmen(string kontenrahmen = "SKR03")
    {
        _settingsRepo.Setup(r => r.GetAsync())
            .ReturnsAsync(new SaldoSettings { Kontenrahmen = kontenrahmen });
    }

    private static InvoiceDto CreateInvoice(
        int id, string number, string customer,
        DateTime invoiceDate, DateTime? paidDate,
        decimal net, decimal vatRate, decimal vat)
    {
        return new InvoiceDto
        {
            Id = id,
            InvoiceNumber = number,
            CustomerName = customer,
            InvoiceDate = invoiceDate,
            PaidDate = paidDate,
            Status = "Paid",
            Items = new List<InvoiceItemDto>
            {
                new() { VatRate = vatRate, TotalNet = net, TotalVat = vat, TotalGross = net + vat }
            },
            TotalNetAfterDiscount = net,
            TotalVat = vat,
            TotalGross = net + vat
        };
    }

    // ─── GetEinnahmenAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetEinnahmen_GibtBuchungenMitTypEinnahmeZurueck()
    {
        SetupKontenrahmen();
        _kontoRepo.Setup(r => r.GetByKontenrahmenAsync("SKR03")).ReturnsAsync(new List<Konto>());
        _fakturaClient.Setup(c => c.GetAllInvoicesAsync(It.IsAny<InvoiceFilterDto>()))
            .ReturnsAsync(new List<InvoiceDto>
            {
                CreateInvoice(1, "RE-001", "Kunde A", new DateTime(2026, 1, 10), new DateTime(2026, 1, 15), 1000m, 19m, 190m)
            });

        var service = CreateService();
        var result = await service.GetEinnahmenAsync(Von, Bis);

        result.Should().HaveCount(1);
        result[0].Typ.Should().Be(BuchungsTyp.Einnahme);
        result[0].Quelle.Should().Be("Faktura");
    }

    [Fact]
    public async Task GetEinnahmen_SortiertNachZahlungsDatum()
    {
        SetupKontenrahmen();
        _kontoRepo.Setup(r => r.GetByKontenrahmenAsync("SKR03")).ReturnsAsync(new List<Konto>());
        _fakturaClient.Setup(c => c.GetAllInvoicesAsync(It.IsAny<InvoiceFilterDto>()))
            .ReturnsAsync(new List<InvoiceDto>
            {
                CreateInvoice(1, "RE-002", "Kunde B", new DateTime(2026, 3, 1), new DateTime(2026, 3, 15), 500m, 19m, 95m),
                CreateInvoice(2, "RE-001", "Kunde A", new DateTime(2026, 1, 1), new DateTime(2026, 1, 5), 1000m, 19m, 190m)
            });

        var service = CreateService();
        var result = await service.GetEinnahmenAsync(Von, Bis);

        result[0].ZahlungsDatum.Should().Be(new DateOnly(2026, 1, 5));
        result[1].ZahlungsDatum.Should().Be(new DateOnly(2026, 3, 15));
    }

    [Fact]
    public async Task GetEinnahmen_NutztKontoAusDBFuerUstSatz()
    {
        SetupKontenrahmen();
        _kontoRepo.Setup(r => r.GetByKontenrahmenAsync("SKR03"))
            .ReturnsAsync(new List<Konto>
            {
                new() { KontoNummer = "8410", KontoBezeichnung = "Erlöse 19% (custom)", KontoTyp = KontoTyp.Einnahme, UstSatz = 19 }
            });
        _fakturaClient.Setup(c => c.GetAllInvoicesAsync(It.IsAny<InvoiceFilterDto>()))
            .ReturnsAsync(new List<InvoiceDto>
            {
                CreateInvoice(1, "RE-001", "Kunde", new DateTime(2026, 1, 1), new DateTime(2026, 1, 1), 1000m, 19m, 190m)
            });

        var service = CreateService();
        var result = await service.GetEinnahmenAsync(Von, Bis);

        result[0].KontoNummer.Should().Be("8410");
        result[0].KontoBezeichnung.Should().Be("Erlöse 19% (custom)");
    }

    [Fact]
    public async Task GetEinnahmen_NutztFallbackKonto8400WennKeinPassendesKonto()
    {
        SetupKontenrahmen();
        _kontoRepo.Setup(r => r.GetByKontenrahmenAsync("SKR03")).ReturnsAsync(new List<Konto>());
        _fakturaClient.Setup(c => c.GetAllInvoicesAsync(It.IsAny<InvoiceFilterDto>()))
            .ReturnsAsync(new List<InvoiceDto>
            {
                CreateInvoice(1, "RE-001", "Kunde", new DateTime(2026, 1, 1), new DateTime(2026, 1, 1), 1000m, 19m, 190m)
            });

        var service = CreateService();
        var result = await service.GetEinnahmenAsync(Von, Bis);

        result[0].KontoNummer.Should().Be("8400");
    }

    [Fact]
    public async Task GetEinnahmen_ErstelltEineBuchungProInvoiceItem()
    {
        SetupKontenrahmen();
        _kontoRepo.Setup(r => r.GetByKontenrahmenAsync("SKR03")).ReturnsAsync(new List<Konto>());
        _fakturaClient.Setup(c => c.GetAllInvoicesAsync(It.IsAny<InvoiceFilterDto>()))
            .ReturnsAsync(new List<InvoiceDto>
            {
                new()
                {
                    Id = 1, InvoiceNumber = "RE-001", InvoiceDate = new DateTime(2026, 1, 1),
                    PaidDate = new DateTime(2026, 1, 1),
                    Items = new List<InvoiceItemDto>
                    {
                        new() { VatRate = 19m, TotalNet = 1000m, TotalVat = 190m, TotalGross = 1190m },
                        new() { VatRate = 7m,  TotalNet = 500m,  TotalVat = 35m,  TotalGross = 535m  }
                    }
                }
            });

        var service = CreateService();
        var result = await service.GetEinnahmenAsync(Von, Bis);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetEinnahmen_NutztInvoiceDateAlsZahlungsdatumWennPaidDateNull()
    {
        SetupKontenrahmen();
        _kontoRepo.Setup(r => r.GetByKontenrahmenAsync("SKR03")).ReturnsAsync(new List<Konto>());
        _fakturaClient.Setup(c => c.GetAllInvoicesAsync(It.IsAny<InvoiceFilterDto>()))
            .ReturnsAsync(new List<InvoiceDto>
            {
                CreateInvoice(1, "RE-001", "Kunde", new DateTime(2026, 2, 15), null, 1000m, 19m, 190m)
            });

        var service = CreateService();
        var result = await service.GetEinnahmenAsync(Von, Bis);

        result[0].ZahlungsDatum.Should().Be(new DateOnly(2026, 2, 15));
    }

    [Fact]
    public async Task GetEinnahmen_GibtLeereListeBeiApiException()
    {
        SetupKontenrahmen();
        _kontoRepo.Setup(r => r.GetByKontenrahmenAsync("SKR03")).ReturnsAsync(new List<Konto>());
        _fakturaClient.Setup(c => c.GetAllInvoicesAsync(It.IsAny<InvoiceFilterDto>()))
            .ThrowsAsync(new HttpRequestException("Verbindungsfehler"));

        var service = CreateService();
        var result = await service.GetEinnahmenAsync(Von, Bis);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEinnahmen_VerwendetKundenNameAlsBeschreibung()
    {
        SetupKontenrahmen();
        _kontoRepo.Setup(r => r.GetByKontenrahmenAsync("SKR03")).ReturnsAsync(new List<Konto>());
        _fakturaClient.Setup(c => c.GetAllInvoicesAsync(It.IsAny<InvoiceFilterDto>()))
            .ReturnsAsync(new List<InvoiceDto>
            {
                CreateInvoice(1, "RE-001", "Musterfirma GmbH", new DateTime(2026, 1, 1), new DateTime(2026, 1, 1), 1000m, 19m, 190m)
            });

        var service = CreateService();
        var result = await service.GetEinnahmenAsync(Von, Bis);

        result[0].Beschreibung.Should().Be("Musterfirma GmbH");
        result[0].QuelleId.Should().Be("RE-001");
    }

    [Fact]
    public async Task GetEinnahmen_FilterNachStatusPaidWirdAnFakturaUebergeben()
    {
        SetupKontenrahmen();
        _kontoRepo.Setup(r => r.GetByKontenrahmenAsync("SKR03")).ReturnsAsync(new List<Konto>());
        _fakturaClient.Setup(c => c.GetAllInvoicesAsync(It.Is<InvoiceFilterDto>(f =>
            f.Status == "Paid" && f.PaidFrom != null && f.PaidTo != null)))
            .ReturnsAsync(new List<InvoiceDto>());

        var service = CreateService();
        await service.GetEinnahmenAsync(Von, Bis);

        _fakturaClient.Verify(c => c.GetAllInvoicesAsync(It.Is<InvoiceFilterDto>(f =>
            f.Status == "Paid" && f.PaidFrom != null && f.PaidTo != null)), Times.Once);
    }

    // ─── GetSummeAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSumme_SummiertTotalNetAfterDiscount()
    {
        SetupKontenrahmen();
        _fakturaClient.Setup(c => c.GetAllInvoicesAsync(It.IsAny<InvoiceFilterDto>()))
            .ReturnsAsync(new List<InvoiceDto>
            {
                new() { TotalNetAfterDiscount = 1000m, Items = new(), PaidDate = new DateTime(2026, 1, 1) },
                new() { TotalNetAfterDiscount = 500m,  Items = new(), PaidDate = new DateTime(2026, 2, 1) }
            });

        var service = CreateService();
        var result = await service.GetSummeAsync(Von, Bis);

        result.Should().Be(1500m);
    }

    [Fact]
    public async Task GetSumme_GibtNullBeiLeeremErgebnis()
    {
        SetupKontenrahmen();
        _fakturaClient.Setup(c => c.GetAllInvoicesAsync(It.IsAny<InvoiceFilterDto>()))
            .ReturnsAsync(new List<InvoiceDto>());

        var service = CreateService();
        var result = await service.GetSummeAsync(Von, Bis);

        result.Should().Be(0m);
    }

    // ─── GetNachUstSatzAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetNachUstSatz_GruppiertKorrektNachUstSatz()
    {
        SetupKontenrahmen();
        _fakturaClient.Setup(c => c.GetAllInvoicesAsync(It.IsAny<InvoiceFilterDto>()))
            .ReturnsAsync(new List<InvoiceDto>
            {
                new()
                {
                    PaidDate = new DateTime(2026, 1, 1),
                    Items = new List<InvoiceItemDto>
                    {
                        new() { VatRate = 19m, TotalNet = 1000m, TotalVat = 190m, TotalGross = 1190m },
                        new() { VatRate = 19m, TotalNet = 500m,  TotalVat = 95m,  TotalGross = 595m  },
                        new() { VatRate = 7m,  TotalNet = 200m,  TotalVat = 14m,  TotalGross = 214m  }
                    }
                }
            });

        var service = CreateService();
        var result = await service.GetNachUstSatzAsync(Von, Bis);

        result.Should().ContainKey("19%").WhoseValue.Should().Be(1500m);
        result.Should().ContainKey("7%").WhoseValue.Should().Be(200m);
    }
}
