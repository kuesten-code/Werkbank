using FluentAssertions;
using Kuestencode.Werkbank.Locatio.Domain.Entities;
using Kuestencode.Werkbank.Locatio.Domain.Enums;
using Kuestencode.Werkbank.Locatio.Services;
using Xunit;

namespace Kuestencode.Werkbank.Locatio.Tests.Services;

public class SaldoBuchungServiceTests
{
    private static readonly DateOnly Von = new(2026, 1, 1);
    private static readonly DateOnly Bis = new(2026, 12, 31);

    private readonly TestDaten _daten = new();
    private readonly SaldoBuchungService _service;

    public SaldoBuchungServiceTests()
    {
        _service = new SaldoBuchungService(_daten.Factory);
    }

    [Fact]
    public async Task Darlehenszahlung_NurZinsanteilWirdUebergeben()
    {
        var gebaeude = _daten.Gebaeude();
        var darlehen = new Darlehen { Id = Guid.NewGuid(), GebaeudeId = gebaeude.Id, Bezeichnung = "Sparkasse 1", Kreditgeber = "Sparkasse" };
        var rate = new Darlehenszahlung { Id = Guid.NewGuid(), DarlehenId = darlehen.Id, Zahlungsdatum = new(2026, 3, 30), Zinsanteil = 312.50m, Tilgungsanteil = 687.50m };
        _daten.Speichere(darlehen, rate);

        var buchungen = await _service.GetBuchungenAsync(Von, Bis);

        var buchung = buchungen.Should().ContainSingle().Subject;
        buchung.IstEinnahme.Should().BeFalse();
        buchung.Netto.Should().Be(312.50m);
        buchung.Brutto.Should().Be(312.50m);
        buchung.Kategorie.Should().Be("Darlehenszinsen");
        buchung.KontoNummer.Should().Be("2120");
    }

    [Fact]
    public async Task Darlehenszahlung_NurTilgung_ErzeugtKeineBuchung()
    {
        var gebaeude = _daten.Gebaeude();
        var darlehen = new Darlehen { Id = Guid.NewGuid(), GebaeudeId = gebaeude.Id, Bezeichnung = "KfW" };
        _daten.Speichere(darlehen, new Darlehenszahlung { Id = Guid.NewGuid(), DarlehenId = darlehen.Id, Zahlungsdatum = new(2026, 3, 30), Tilgungsanteil = 500m });

        var buchungen = await _service.GetBuchungenAsync(Von, Bis);

        buchungen.Should().BeEmpty();
    }

    [Fact]
    public async Task Kostenposten_KategorieTilgung_WirdNichtUebergeben()
    {
        var gebaeude = _daten.Gebaeude();
        _daten.Speichere(Kosten(gebaeude.Id, _daten.Tilgung.Id, 1000m), Kosten(gebaeude.Id, _daten.Nebenkosten.Id, 80m));

        var buchungen = await _service.GetBuchungenAsync(Von, Bis);

        buchungen.Should().ContainSingle().Which.Netto.Should().Be(80m);
    }

    [Fact]
    public async Task Kostenposten_UebergibtGetragenenAnteilStattGesamtbetrag()
    {
        var gebaeude = _daten.Gebaeude();
        var posten = Kosten(gebaeude.Id, _daten.Nebenkosten.Id, 240m);
        posten.Gesamtbetrag = 1200m;
        posten.Verteilschluessel = "MEA 200/1000";
        _daten.Speichere(posten);

        var buchung = (await _service.GetBuchungenAsync(Von, Bis)).Should().ContainSingle().Subject;

        buchung.Netto.Should().Be(240m);
        buchung.Brutto.Should().Be(240m);
        buchung.Ust.Should().Be(0m);
        buchung.KontoNummer.Should().Be("2350");
    }

    [Fact]
    public async Task Mieteingang_GewerbeMitUstOption_TrenntNettoUndUst()
    {
        var einheit = TestDaten.Einheit("Laden", art: EinheitArt.Gewerbe, kaltmiete: 1000m, nebenkosten: 200m);
        einheit.UstOption = true;
        _daten.Gebaeude(einheiten: [einheit]);
        _daten.Speichere(Miete(einheit.Id, 1000m, 200m, ustSatz: 19m));

        var buchung = (await _service.GetBuchungenAsync(Von, Bis)).Should().ContainSingle().Subject;

        buchung.IstEinnahme.Should().BeTrue();
        buchung.Netto.Should().Be(1200m);
        buchung.Ust.Should().Be(228m);
        buchung.Brutto.Should().Be(1428m);
        buchung.UstSatz.Should().Be(19m);
    }

    [Fact]
    public async Task Mieteingang_OhneUstOption_BruttoGleichNetto()
    {
        var einheit = TestDaten.Einheit("Whg 1");
        _daten.Gebaeude(einheiten: [einheit]);
        _daten.Speichere(Miete(einheit.Id, 800m, 200m, ustSatz: 0m));

        var buchung = (await _service.GetBuchungenAsync(Von, Bis)).Should().ContainSingle().Subject;

        buchung.Netto.Should().Be(1000m);
        buchung.Brutto.Should().Be(1000m);
        buchung.Ust.Should().Be(0m);
        buchung.Kategorie.Should().Be("Mieteinnahmen");
    }

    [Fact]
    public async Task NkAbrechnung_NachzahlungIstEinnahme_ErstattungIstAusgabe()
    {
        var whg1 = TestDaten.Einheit("Whg 1");
        var whg2 = TestDaten.Einheit("Whg 2");
        _daten.Gebaeude(einheiten: [whg1, whg2]);
        _daten.Speichere(
            new Nebenkostenabrechnung { Id = Guid.NewGuid(), MieteinheitId = whg1.Id, Jahr = 2025, Ergebnis = 150m, Zahlungsdatum = new(2026, 5, 2) },
            new Nebenkostenabrechnung { Id = Guid.NewGuid(), MieteinheitId = whg2.Id, Jahr = 2025, Ergebnis = -90m, Zahlungsdatum = new(2026, 5, 3) });

        var buchungen = await _service.GetBuchungenAsync(Von, Bis);

        buchungen.Should().HaveCount(2);
        buchungen.Single(b => b.IstEinnahme).Netto.Should().Be(150m);
        buchungen.Single(b => !b.IstEinnahme).Netto.Should().Be(90m);
    }

    [Fact]
    public async Task NkAbrechnung_GewerbeMitUstOption_RechnetUstHeraus()
    {
        var laden = TestDaten.Einheit("Laden", art: EinheitArt.Gewerbe);
        laden.UstOption = true;
        _daten.Gebaeude(einheiten: [laden]);
        _daten.Speichere(new Nebenkostenabrechnung { Id = Guid.NewGuid(), MieteinheitId = laden.Id, Jahr = 2025, Ergebnis = 119m, Zahlungsdatum = new(2026, 5, 2) });

        var buchung = (await _service.GetBuchungenAsync(Von, Bis)).Should().ContainSingle().Subject;

        buchung.Netto.Should().Be(100m);
        buchung.Ust.Should().Be(19m);
        buchung.Brutto.Should().Be(119m);
    }

    [Fact]
    public async Task Zeitraum_FiltertNachZahlungsdatum()
    {
        var gebaeude = _daten.Gebaeude();
        var dezember = Kosten(gebaeude.Id, _daten.Nebenkosten.Id, 50m);
        dezember.Zahlungsdatum = new DateOnly(2025, 12, 31);
        var januar = Kosten(gebaeude.Id, _daten.Nebenkosten.Id, 70m);
        januar.Zahlungsdatum = new DateOnly(2026, 1, 1);
        _daten.Speichere(dezember, januar);

        var buchungen = await _service.GetBuchungenAsync(Von, Bis);

        buchungen.Should().ContainSingle().Which.Netto.Should().Be(70m);
    }

    [Fact]
    public async Task GetBuchungenMitGebaeude_OrdnetBuchungDemGebaeudeZu()
    {
        var einheit = TestDaten.Einheit("Whg 1");
        var gebaeude = _daten.Gebaeude(einheiten: [einheit]);
        _daten.Speichere(Miete(einheit.Id, 800m, 200m, 0m));

        var buchungen = await _service.GetBuchungenMitGebaeudeAsync(Von, Bis);

        buchungen.Should().ContainSingle().Which.GebaeudeId.Should().Be(gebaeude.Id);
    }

    private static Kostenposten Kosten(Guid gebaeudeId, Guid kategorieId, decimal betrag) => new()
    {
        Id = Guid.NewGuid(),
        GebaeudeId = gebaeudeId,
        KategorieId = kategorieId,
        Betrag = betrag,
        Zahlungsdatum = new DateOnly(2026, 4, 15),
        Beschreibung = "Test"
    };

    private static Mieteingang Miete(Guid einheitId, decimal kalt, decimal nk, decimal ustSatz) => new()
    {
        Id = Guid.NewGuid(),
        MieteinheitId = einheitId,
        Zahlungsdatum = new DateOnly(2026, 3, 3),
        Monat = new DateOnly(2026, 3, 1),
        Kaltmiete = kalt,
        Nebenkosten = nk,
        UstSatz = ustSatz
    };
}
