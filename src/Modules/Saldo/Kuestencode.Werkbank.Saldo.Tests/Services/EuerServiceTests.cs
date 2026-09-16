using FluentAssertions;
using Kuestencode.Core.Interfaces;
using Kuestencode.Core.Models;
using Kuestencode.Werkbank.Saldo.Data.Repositories;
using Kuestencode.Werkbank.Saldo.Domain.Dtos;
using Kuestencode.Werkbank.Saldo.Domain.Entities;
using Kuestencode.Werkbank.Saldo.Domain.Enums;
using Kuestencode.Werkbank.Saldo.Services;
using Moq;
using Xunit;

namespace Kuestencode.Werkbank.Saldo.Tests.Services;

public class EuerServiceTests
{
    private readonly Mock<IEinnahmenService> _einnahmen = new();
    private readonly Mock<IAusgabenService> _ausgaben = new();
    private readonly Mock<IKontoRepository> _kontoRepo = new();
    private readonly Mock<ISaldoSettingsRepository> _settingsRepo = new();
    private readonly Mock<ICompanyService> _companyService = new();

    private static readonly DateOnly Von = new(2026, 1, 1);
    private static readonly DateOnly Bis = new(2026, 12, 31);

    public EuerServiceTests()
    {
        SetupKontenrahmen();
        _kontoRepo.Setup(r => r.GetByKontenrahmenAsync("SKR03")).ReturnsAsync(new List<Konto>());
        _einnahmen.Setup(s => s.GetEinnahmenAsync(Von, Bis)).ReturnsAsync(new List<BuchungDto>());
        _ausgaben.Setup(s => s.GetAusgabenAsync(Von, Bis)).ReturnsAsync(new List<BuchungDto>());
    }

    private EuerService CreateService() =>
        new(_einnahmen.Object, _ausgaben.Object, _kontoRepo.Object, _settingsRepo.Object, _companyService.Object);

    private void SetupKontenrahmen(string kontenrahmen = "SKR03")
    {
        _settingsRepo.Setup(r => r.GetAsync())
            .ReturnsAsync(new SaldoSettings { Kontenrahmen = kontenrahmen });
    }

    // ─── Zufluss-/Abflussprinzip auf Zahlungsebene ─────────────────────────────

    [Fact]
    public async Task GetEuerSummary_TeilzahlungAusVorperiodeWirdNichtDoppeltGezaehlt()
    {
        // Anzahlung wurde bereits 2025 gezahlt (liegt außerhalb des Zeitraums) und taucht
        // deshalb hier nicht auf; nur die Restzahlung aus 2026 darf in der EÜR erscheinen.
        // Das ist genau der Fall, den die alte belegbasierte Aggregation (volle Rechnungssumme
        // zum PaidDate der Schlusszahlung) falsch verbucht hat.
        _einnahmen.Setup(s => s.GetEinnahmenAsync(Von, Bis))
            .ReturnsAsync(new List<BuchungDto>
            {
                new()
                {
                    Netto = 200m, Ust = 38m, Brutto = 238m, UstSatz = 19,
                    Typ = BuchungsTyp.Einnahme, KontoNummer = "8400", KontoBezeichnung = "Erlöse 19%"
                }
            });

        var service = CreateService();
        var result = await service.GetEuerSummaryAsync(new EuerFilterDto { Von = Von, Bis = Bis });

        result.EinnahmenNetto.Should().Be(200m); // nur die Restzahlung, nicht die volle Rechnungssumme
        result.EinnahmenMwst.Should().Be(38m);
    }

    // ─── Aggregation nach Konten ────────────────────────────────────────────────

    [Fact]
    public async Task GetEuerSummary_GruppiertEinnahmenNachKonto()
    {
        _kontoRepo.Setup(r => r.GetByKontenrahmenAsync("SKR03"))
            .ReturnsAsync(new List<Konto>
            {
                new() { KontoNummer = "8400", KontoBezeichnung = "Erlöse 19%", KontoTyp = KontoTyp.Einnahme, UstSatz = 19 },
                new() { KontoNummer = "8300", KontoBezeichnung = "Erlöse 7%",  KontoTyp = KontoTyp.Einnahme, UstSatz = 7  }
            });
        _einnahmen.Setup(s => s.GetEinnahmenAsync(Von, Bis))
            .ReturnsAsync(new List<BuchungDto>
            {
                new() { Netto = 1000m, Ust = 190m, Brutto = 1190m, UstSatz = 19, Typ = BuchungsTyp.Einnahme, KontoNummer = "8400" },
                new() { Netto = 500m,  Ust = 35m,  Brutto = 535m,  UstSatz = 7,  Typ = BuchungsTyp.Einnahme, KontoNummer = "8300" }
            });

        var service = CreateService();
        var result = await service.GetEuerSummaryAsync(new EuerFilterDto { Von = Von, Bis = Bis });

        result.Einnahmen.Should().HaveCount(2);
        result.Einnahmen.Sum(p => p.BetragNetto).Should().Be(1500m);

        var pos19 = result.Einnahmen.Single(p => p.KontoNummer == "8400");
        pos19.BetragNetto.Should().Be(1000m);
        pos19.MwstBetrag.Should().Be(190m);
        pos19.Gruppe.Should().Be("Erlöse");

        var pos7 = result.Einnahmen.Single(p => p.KontoNummer == "8300");
        pos7.BetragNetto.Should().Be(500m);
    }

    [Fact]
    public async Task GetEuerSummary_GruppiertAusgabenNachKontoUndZaehltBuchungen()
    {
        _kontoRepo.Setup(r => r.GetByKontenrahmenAsync("SKR03"))
            .ReturnsAsync(new List<Konto>
            {
                new() { KontoNummer = "4930", KontoBezeichnung = "Bürobedarf",  KontoTyp = KontoTyp.Ausgabe },
                new() { KontoNummer = "4660", KontoBezeichnung = "Reisekosten", KontoTyp = KontoTyp.Ausgabe }
            });
        _ausgaben.Setup(s => s.GetAusgabenAsync(Von, Bis))
            .ReturnsAsync(new List<BuchungDto>
            {
                new() { Netto = 100m, Ust = 19m,   Brutto = 119m,   Typ = BuchungsTyp.Ausgabe, KontoNummer = "4930" },
                new() { Netto = 200m, Ust = 38m,   Brutto = 238m,   Typ = BuchungsTyp.Ausgabe, KontoNummer = "4930" },
                new() { Netto = 150m, Ust = 28.5m, Brutto = 178.5m, Typ = BuchungsTyp.Ausgabe, KontoNummer = "4660" }
            });

        var service = CreateService();
        var result = await service.GetEuerSummaryAsync(new EuerFilterDto { Von = Von, Bis = Bis });

        result.Ausgaben.Should().HaveCount(2);

        var buero = result.Ausgaben.Single(p => p.KontoNummer == "4930");
        buero.BetragNetto.Should().Be(300m); // 100 + 200
        buero.AnzahlBelege.Should().Be(2);
        buero.Gruppe.Should().Be("Betriebsausgaben");

        var reise = result.Ausgaben.Single(p => p.KontoNummer == "4660");
        reise.BetragNetto.Should().Be(150m);
        reise.AnzahlBelege.Should().Be(1);
    }

    [Fact]
    public async Task GetEuerSummary_UnbekanntesKontoNutztBuchungAlsFallback()
    {
        _ausgaben.Setup(s => s.GetAusgabenAsync(Von, Bis))
            .ReturnsAsync(new List<BuchungDto>
            {
                new()
                {
                    Netto = 100m, Ust = 0m, Brutto = 100m, Typ = BuchungsTyp.Ausgabe,
                    KontoNummer = "4980", KontoBezeichnung = "Sonstige Betriebsausgaben"
                }
            });

        var service = CreateService();
        var result = await service.GetEuerSummaryAsync(new EuerFilterDto { Von = Von, Bis = Bis });

        result.Ausgaben.Should().HaveCount(1);
        result.Ausgaben[0].KontoNummer.Should().Be("4980");
        result.Ausgaben[0].KontoBezeichnung.Should().Be("Sonstige Betriebsausgaben");
        result.Ausgaben[0].Gruppe.Should().Be("Sonstiges");
    }

    // ─── Edge Cases ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetEuerSummary_LeererZeitraum_GibtLeereListenZurueck()
    {
        var service = CreateService();
        var result = await service.GetEuerSummaryAsync(new EuerFilterDto { Von = Von, Bis = Bis });

        result.EinnahmenNetto.Should().Be(0m);
        result.AusgabenNetto.Should().Be(0m);
        result.Ueberschuss.Should().Be(0m);
        result.Einnahmen.Should().BeEmpty();
        result.Ausgaben.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEuerSummary_KleinunternehmerFlagWirdVonCompanyServiceUebernommen()
    {
        _companyService.Setup(s => s.GetCompanyAsync()).ReturnsAsync(new Company { IsKleinunternehmer = true });

        var service = CreateService();
        var result = await service.GetEuerSummaryAsync(new EuerFilterDto { Von = Von, Bis = Bis });

        result.IstKleinunternehmer.Should().BeTrue();
    }

    [Fact]
    public async Task GetEuerSummary_CompanyServiceFehlerFuehrtNichtZuAusnahme()
    {
        _companyService.Setup(s => s.GetCompanyAsync()).ThrowsAsync(new HttpRequestException("Host nicht erreichbar"));

        var service = CreateService();
        var result = await service.GetEuerSummaryAsync(new EuerFilterDto { Von = Von, Bis = Bis });

        result.IstKleinunternehmer.Should().BeFalse();
    }

    // ─── Berechnung Überschuss ──────────────────────────────────────────────────

    [Fact]
    public async Task GetEuerSummary_UeberschussBerechnetKorrekt()
    {
        _einnahmen.Setup(s => s.GetEinnahmenAsync(Von, Bis))
            .ReturnsAsync(new List<BuchungDto>
            {
                new() { Netto = 5000m, Ust = 950m, Brutto = 5950m, Typ = BuchungsTyp.Einnahme, KontoNummer = "8400" }
            });
        _ausgaben.Setup(s => s.GetAusgabenAsync(Von, Bis))
            .ReturnsAsync(new List<BuchungDto>
            {
                new() { Netto = 2000m, Ust = 380m, Brutto = 2380m, Typ = BuchungsTyp.Ausgabe, KontoNummer = "4930" }
            });

        var service = CreateService();
        var result = await service.GetEuerSummaryAsync(new EuerFilterDto { Von = Von, Bis = Bis });

        result.EinnahmenNetto.Should().Be(5000m);
        result.AusgabenNetto.Should().Be(2000m);
        result.Ueberschuss.Should().Be(3000m);
    }
}
