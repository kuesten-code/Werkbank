using FluentAssertions;
using Kuestencode.Werkbank.Locatio.Domain.Dtos;
using Kuestencode.Werkbank.Locatio.Domain.Entities;
using Kuestencode.Werkbank.Locatio.Domain.Enums;
using Kuestencode.Werkbank.Locatio.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Kuestencode.Werkbank.Locatio.Tests.Services;

public class VeraeusserungServiceTests
{
    private static readonly DateOnly Verkauf = new(2026, 7, 1);

    private readonly TestDaten _daten = new();
    private readonly VeraeusserungService _service;

    public VeraeusserungServiceTests()
    {
        _service = new VeraeusserungService(_daten.Factory);
    }

    [Fact]
    public async Task VeraeussereEinheit_BeendetLaufendesMietverhaeltnisMitWarnung()
    {
        var einheit = TestDaten.Einheit("Whg 3");
        _daten.Gebaeude(einheiten: [einheit]);

        var warnungen = await _service.VeraeussereEinheitAsync(einheit.Id, new VeraeusserungDaten(Verkauf, 250_000m, 9_000m));

        warnungen.Should().ContainSingle().Which.Should()
            .Contain("Mieter Whg 3").And.Contain("30.06.2026").And.Contain("bisher unbefristet");

        var gespeichert = await LadeEinheitAsync(einheit.Id);
        gespeichert.Status.Should().Be(EinheitStatus.Veraeussert);
        gespeichert.VerkaufsDatum.Should().Be(Verkauf);
        gespeichert.VertragsEnde.Should().Be(new DateOnly(2026, 6, 30));
        gespeichert.Veraeusserungspreis.Should().Be(250_000m);
        gespeichert.Veraeusserungskosten.Should().Be(9_000m);
    }

    [Fact]
    public async Task VeraeussereEinheit_SperrtBuchungenAbVerkaufsdatum()
    {
        var einheit = TestDaten.Einheit("Whg 3");
        _daten.Gebaeude(einheiten: [einheit]);
        await _service.VeraeussereEinheitAsync(einheit.Id, new VeraeusserungDaten(Verkauf, null, null));

        var mieten = new MieteingangService(_daten.Factory);
        var kosten = new KostenService(_daten.Factory);
        var nk = new NebenkostenabrechnungService(_daten.Factory);

        var miete = () => mieten.SaveAsync(new Mieteingang { MieteinheitId = einheit.Id, Zahlungsdatum = Verkauf, Monat = Verkauf, Kaltmiete = 800m });
        var posten = () => kosten.SaveAsync(new Kostenposten { MieteinheitId = einheit.Id, KategorieId = _daten.Nebenkosten.Id, Betrag = 10m, Zahlungsdatum = Verkauf.AddDays(5), Beschreibung = "Strom" });
        var abrechnung = () => nk.SaveAsync(new Nebenkostenabrechnung { MieteinheitId = einheit.Id, Jahr = 2026, Ergebnis = 50m, Zahlungsdatum = Verkauf });

        await miete.Should().ThrowAsync<InvalidOperationException>().WithMessage("*seit 01.07.2026 veräußert*");
        await posten.Should().ThrowAsync<InvalidOperationException>().WithMessage("*veräußert*");
        await abrechnung.Should().ThrowAsync<InvalidOperationException>().WithMessage("*veräußert*");

        var vorVerkauf = await mieten.SaveAsync(new Mieteingang
        {
            MieteinheitId = einheit.Id, Zahlungsdatum = Verkauf.AddDays(-1), Monat = new DateOnly(2026, 6, 1), Kaltmiete = 800m
        });
        vorVerkauf.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task VeraeussereEinheit_VertragsendeVorVerkauf_BleibtUnveraendertOhneWarnung()
    {
        var einheit = TestDaten.Einheit("Whg 3", EinheitStatus.Gekuendigt);
        einheit.VertragsEnde = new DateOnly(2026, 3, 31);
        _daten.Gebaeude(einheiten: [einheit]);

        var warnungen = await _service.VeraeussereEinheitAsync(einheit.Id, new VeraeusserungDaten(Verkauf, null, null));

        warnungen.Should().BeEmpty();
        (await LadeEinheitAsync(einheit.Id)).VertragsEnde.Should().Be(new DateOnly(2026, 3, 31));
    }

    [Fact]
    public async Task PruefeEinheit_AendertNichtsUndNenntBisherigesVertragsende()
    {
        var einheit = TestDaten.Einheit("Whg 3");
        einheit.VertragsEnde = new DateOnly(2027, 12, 31);
        _daten.Gebaeude(einheiten: [einheit]);

        var warnungen = await _service.PruefeEinheitAsync(einheit.Id, Verkauf);

        warnungen.Should().ContainSingle().Which.Should().Contain("bisher bis 31.12.2027");
        var unveraendert = await LadeEinheitAsync(einheit.Id);
        unveraendert.Status.Should().Be(EinheitStatus.Vermietet);
        unveraendert.VertragsEnde.Should().Be(new DateOnly(2027, 12, 31));
    }

    [Fact]
    public async Task Pruefe_WarntVorBestehendenBuchungenAbVerkaufsdatum()
    {
        var einheit = TestDaten.Einheit("Whg 3", EinheitStatus.Leer);
        _daten.Gebaeude(einheiten: [einheit]);
        _daten.Speichere(new Mieteingang { Id = Guid.NewGuid(), MieteinheitId = einheit.Id, Zahlungsdatum = Verkauf.AddDays(2), Monat = Verkauf, Kaltmiete = 1m });

        var warnungen = await _service.PruefeEinheitAsync(einheit.Id, Verkauf);

        warnungen.Should().ContainSingle().Which.Should().Contain("1 Buchung(en)");
    }

    [Fact]
    public async Task VeraeussereGebaeude_SetztAlleEigenenEinheitenAufVeraeussert()
    {
        var eigene1 = TestDaten.Einheit("Whg 1");
        var eigene2 = TestDaten.Einheit("Whg 2", EinheitStatus.Leer);
        var fremd = TestDaten.Einheit("Whg 3", EinheitStatus.Fremdeigentum);
        var gebaeude = _daten.Gebaeude(einheiten: [eigene1, eigene2, fremd]);

        var warnungen = await _service.VeraeussereGebaeudeAsync(gebaeude.Id, new VeraeusserungDaten(Verkauf, 900_000m, 30_000m));

        warnungen.Should().ContainSingle();
        await using var db = _daten.Factory.CreateDbContext();
        var gespeichert = await db.Gebaeude.Include(g => g.Einheiten).SingleAsync();
        gespeichert.VerkaufsDatum.Should().Be(Verkauf);
        gespeichert.Veraeusserungspreis.Should().Be(900_000m);
        gespeichert.Einheiten.Single(e => e.Id == eigene1.Id).Status.Should().Be(EinheitStatus.Veraeussert);
        gespeichert.Einheiten.Single(e => e.Id == eigene2.Id).Status.Should().Be(EinheitStatus.Veraeussert);
        gespeichert.Einheiten.Single(e => e.Id == fremd.Id).Status.Should().Be(EinheitStatus.Fremdeigentum);
    }

    [Fact]
    public async Task VeraeussereGebaeude_SperrtGebaeudekostenUndDarlehenszinsen()
    {
        var gebaeude = _daten.Gebaeude();
        var darlehen = new Darlehen { Id = Guid.NewGuid(), GebaeudeId = gebaeude.Id, Bezeichnung = "KfW" };
        _daten.Speichere(darlehen);
        await _service.VeraeussereGebaeudeAsync(gebaeude.Id, new VeraeusserungDaten(Verkauf, null, null));

        var kosten = () => new KostenService(_daten.Factory).SaveAsync(new Kostenposten
        {
            GebaeudeId = gebaeude.Id, KategorieId = _daten.Nebenkosten.Id, Betrag = 10m, Zahlungsdatum = Verkauf, Beschreibung = "Gärtner"
        });
        var zins = () => new DarlehenService(_daten.Factory).SaveZahlungAsync(new Darlehenszahlung
        {
            DarlehenId = darlehen.Id, Zahlungsdatum = Verkauf.AddMonths(1), Zinsanteil = 100m
        });

        await kosten.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Gebäude*veräußert*");
        await zins.Should().ThrowAsync<InvalidOperationException>().WithMessage("*veräußert*");
    }

    [Fact]
    public async Task VeraeussereGebaeude_ZweimalVerkaufen_WirftException()
    {
        var gebaeude = _daten.Gebaeude();
        await _service.VeraeussereGebaeudeAsync(gebaeude.Id, new VeraeusserungDaten(Verkauf, null, null));

        var act = () => _service.PruefeGebaeudeAsync(gebaeude.Id, Verkauf);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*bereits veräußert*");
    }

    [Theory]
    [InlineData(EinheitStatus.Fremdeigentum, "Fremdeigentum*")]
    [InlineData(EinheitStatus.Veraeussert, "*bereits veräußert*")]
    public async Task VeraeussereEinheit_NichtVerkaufbar_WirftException(EinheitStatus status, string meldung)
    {
        var einheit = TestDaten.Einheit("Whg 3", status);
        _daten.Gebaeude(einheiten: [einheit]);

        var act = () => _service.VeraeussereEinheitAsync(einheit.Id, new VeraeusserungDaten(Verkauf, null, null));

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage(meldung);
    }

    [Fact]
    public async Task VeraeussereEinheit_Unbekannt_WirftException()
    {
        var act = () => _service.PruefeEinheitAsync(Guid.NewGuid(), Verkauf);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Einheit nicht gefunden.");
    }

    private async Task<Mieteinheit> LadeEinheitAsync(Guid id)
    {
        await using var db = _daten.Factory.CreateDbContext();
        return await db.Mieteinheiten.SingleAsync(e => e.Id == id);
    }
}
