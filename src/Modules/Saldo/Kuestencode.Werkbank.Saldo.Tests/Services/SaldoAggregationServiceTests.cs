using FluentAssertions;
using Kuestencode.Werkbank.Saldo.Domain.Dtos;
using Kuestencode.Werkbank.Saldo.Domain.Enums;
using Kuestencode.Werkbank.Saldo.Services;
using Moq;
using Xunit;

namespace Kuestencode.Werkbank.Saldo.Tests.Services;

public class SaldoAggregationServiceTests
{
    private readonly Mock<IEinnahmenService> _einnahmen = new();
    private readonly Mock<IAusgabenService> _ausgaben = new();

    private SaldoAggregationService CreateService() =>
        new(_einnahmen.Object, _ausgaben.Object);

    private static readonly DateOnly Von = new(2026, 1, 1);
    private static readonly DateOnly Bis = new(2026, 12, 31);

    // ─── GetUebersichtAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetUebersicht_BerechnetsaldoKorrekt()
    {
        _einnahmen.Setup(s => s.GetEinnahmenAsync(Von, Bis))
            .ReturnsAsync(new List<BuchungDto>
            {
                new() { Netto = 1000m, Ust = 190m,  Brutto = 1190m, UstSatz = 19, Typ = BuchungsTyp.Einnahme },
                new() { Netto = 500m,  Ust = 35m,   Brutto = 535m,  UstSatz = 7,  Typ = BuchungsTyp.Einnahme }
            });
        _ausgaben.Setup(s => s.GetAusgabenAsync(Von, Bis))
            .ReturnsAsync(new List<BuchungDto>
            {
                new() { Netto = 300m, Ust = 57m, Brutto = 357m, UstSatz = 19, Typ = BuchungsTyp.Ausgabe }
            });

        var service = CreateService();
        var result = await service.GetUebersichtAsync(Von, Bis);

        result.Einnahmen.Should().Be(1500m);  // 1000 + 500
        result.Ausgaben.Should().Be(300m);
        result.Saldo.Should().Be(1200m);       // 1500 - 300
        result.Umsatzsteuer.Should().Be(225m); // 190 + 35
        result.Vorsteuer.Should().Be(57m);
        result.UstZahllast.Should().Be(168m);  // 225 - 57
    }

    [Fact]
    public async Task GetUebersicht_LeererZeitraum_GibtNullZurueck()
    {
        _einnahmen.Setup(s => s.GetEinnahmenAsync(Von, Bis)).ReturnsAsync(new List<BuchungDto>());
        _ausgaben.Setup(s => s.GetAusgabenAsync(Von, Bis)).ReturnsAsync(new List<BuchungDto>());

        var service = CreateService();
        var result = await service.GetUebersichtAsync(Von, Bis);

        result.Einnahmen.Should().Be(0m);
        result.Ausgaben.Should().Be(0m);
        result.Saldo.Should().Be(0m);
        result.UstZahllast.Should().Be(0m);
    }

    [Fact]
    public async Task GetUebersicht_VerlustWennAusgabenGroesserAlsEinnahmen()
    {
        _einnahmen.Setup(s => s.GetEinnahmenAsync(Von, Bis))
            .ReturnsAsync(new List<BuchungDto>
            {
                new() { Netto = 500m, Typ = BuchungsTyp.Einnahme }
            });
        _ausgaben.Setup(s => s.GetAusgabenAsync(Von, Bis))
            .ReturnsAsync(new List<BuchungDto>
            {
                new() { Netto = 800m, Typ = BuchungsTyp.Ausgabe }
            });

        var service = CreateService();
        var result = await service.GetUebersichtAsync(Von, Bis);

        result.Saldo.Should().Be(-300m);
    }

    // ─── GetAlleBuchungenAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetAlleBuchungen_SortiertNachZahlungsDatum()
    {
        var einnahme1 = new BuchungDto { ZahlungsDatum = new DateOnly(2026, 3, 15), Typ = BuchungsTyp.Einnahme, Netto = 100m };
        var einnahme2 = new BuchungDto { ZahlungsDatum = new DateOnly(2026, 1, 10), Typ = BuchungsTyp.Einnahme, Netto = 200m };
        var ausgabe1  = new BuchungDto { ZahlungsDatum = new DateOnly(2026, 2, 20), Typ = BuchungsTyp.Ausgabe,  Netto = 50m };

        _einnahmen.Setup(s => s.GetEinnahmenAsync(Von, Bis)).ReturnsAsync(new List<BuchungDto> { einnahme1, einnahme2 });
        _ausgaben.Setup(s => s.GetAusgabenAsync(Von, Bis)).ReturnsAsync(new List<BuchungDto> { ausgabe1 });

        var service = CreateService();
        var result = await service.GetAlleBuchungenAsync(Von, Bis);

        result.Should().HaveCount(3);
        result[0].ZahlungsDatum.Should().Be(new DateOnly(2026, 1, 10));
        result[1].ZahlungsDatum.Should().Be(new DateOnly(2026, 2, 20));
        result[2].ZahlungsDatum.Should().Be(new DateOnly(2026, 3, 15));
    }

    [Fact]
    public async Task GetAlleBuchungen_EnthaltEinnahmenUndAusgaben()
    {
        _einnahmen.Setup(s => s.GetEinnahmenAsync(Von, Bis))
            .ReturnsAsync(new List<BuchungDto>
            {
                new() { Typ = BuchungsTyp.Einnahme, ZahlungsDatum = Von },
                new() { Typ = BuchungsTyp.Einnahme, ZahlungsDatum = Von }
            });
        _ausgaben.Setup(s => s.GetAusgabenAsync(Von, Bis))
            .ReturnsAsync(new List<BuchungDto>
            {
                new() { Typ = BuchungsTyp.Ausgabe, ZahlungsDatum = Von }
            });

        var service = CreateService();
        var result = await service.GetAlleBuchungenAsync(Von, Bis);

        result.Should().HaveCount(3);
        result.Count(b => b.Typ == BuchungsTyp.Einnahme).Should().Be(2);
        result.Count(b => b.Typ == BuchungsTyp.Ausgabe).Should().Be(1);
    }

    // ─── GetUstUebersichtAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetUstUebersicht_GruppiertKorrektNachMonat()
    {
        _einnahmen.Setup(s => s.GetEinnahmenAsync(Von, Bis))
            .ReturnsAsync(new List<BuchungDto>
            {
                new() { ZahlungsDatum = new DateOnly(2026, 1, 5),  Ust = 190m, UstSatz = 19, Typ = BuchungsTyp.Einnahme },
                new() { ZahlungsDatum = new DateOnly(2026, 1, 20), Ust = 35m,  UstSatz = 7,  Typ = BuchungsTyp.Einnahme },
                new() { ZahlungsDatum = new DateOnly(2026, 3, 10), Ust = 95m,  UstSatz = 19, Typ = BuchungsTyp.Einnahme }
            });
        _ausgaben.Setup(s => s.GetAusgabenAsync(Von, Bis))
            .ReturnsAsync(new List<BuchungDto>
            {
                new() { ZahlungsDatum = new DateOnly(2026, 1, 15), Ust = 57m, Typ = BuchungsTyp.Ausgabe }
            });

        var service = CreateService();
        var result = await service.GetUstUebersichtAsync(Von, Bis);

        var januar = result.Monate.Single(m => m.Jahr == 2026 && m.Monat == 1);
        januar.Umsatzsteuer19.Should().Be(190m);
        januar.Umsatzsteuer7.Should().Be(35m);
        januar.VorsteuerGesamt.Should().Be(57m);
        januar.Zahllast.Should().Be(168m); // 190+35-57

        var maerz = result.Monate.Single(m => m.Jahr == 2026 && m.Monat == 3);
        maerz.Umsatzsteuer19.Should().Be(95m);
        maerz.VorsteuerGesamt.Should().Be(0m);
    }

    [Fact]
    public async Task GetUstUebersicht_NurAusgabenMonatWirdEbenfallsErfasst()
    {
        _einnahmen.Setup(s => s.GetEinnahmenAsync(Von, Bis))
            .ReturnsAsync(new List<BuchungDto>()); // keine Einnahmen
        _ausgaben.Setup(s => s.GetAusgabenAsync(Von, Bis))
            .ReturnsAsync(new List<BuchungDto>
            {
                new() { ZahlungsDatum = new DateOnly(2026, 6, 1), Ust = 100m, Typ = BuchungsTyp.Ausgabe }
            });

        var service = CreateService();
        var result = await service.GetUstUebersichtAsync(Von, Bis);

        var juni = result.Monate.Single(m => m.Monat == 6);
        juni.VorsteuerGesamt.Should().Be(100m);
        juni.Umsatzsteuer19.Should().Be(0m);
        juni.Zahllast.Should().Be(-100m); // Erstattung
    }
}
