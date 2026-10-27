using FluentAssertions;
using Kuestencode.Werkbank.Locatio.Domain.Dtos;
using Kuestencode.Werkbank.Locatio.Domain.Entities;
using Kuestencode.Werkbank.Locatio.Domain.Enums;
using Kuestencode.Werkbank.Locatio.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Kuestencode.Werkbank.Locatio.Tests.Services;

/// <summary>
/// Erfassung von Kosten, Mieteingängen, NK-Abrechnungen, Darlehen und Kategorien.
/// </summary>
public class BuchungsServiceTests
{
    private readonly TestDaten _daten = new();

    [Fact]
    public async Task Kosten_ZuordnungZurEinheit_FuehrtGebaeudeMit()
    {
        var einheit = TestDaten.Einheit("Whg 1");
        var gebaeude = _daten.Gebaeude(einheiten: [einheit]);
        var service = new KostenService(_daten.Factory);

        await service.SaveAsync(new Kostenposten
        {
            MieteinheitId = einheit.Id, KategorieId = _daten.Nebenkosten.Id, Betrag = 40m, Gesamtbetrag = 200m,
            Verteilschluessel = "Wohnfläche", Zahlungsdatum = new(2026, 3, 1), Beschreibung = " Strom "
        }, new DateiUpload("beleg.pdf", "application/pdf", [1]));

        var posten = (await service.GetAsync(2026, gebaeude.Id)).Should().ContainSingle().Subject;
        posten.GebaeudeId.Should().Be(gebaeude.Id);
        posten.Beschreibung.Should().Be("Strom");
        posten.BelegId.Should().NotBeNull();
        posten.Kategorie!.Name.Should().Be("Nebenkosten");
    }

    [Fact]
    public async Task Kosten_Validierung()
    {
        var gebaeude = _daten.Gebaeude();
        var service = new KostenService(_daten.Factory);
        Kostenposten Posten() => new()
        {
            GebaeudeId = gebaeude.Id, KategorieId = _daten.Nebenkosten.Id, Betrag = 10m, Zahlungsdatum = new(2026, 1, 1), Beschreibung = "X"
        };

        var ohneZuordnung = Posten(); ohneZuordnung.GebaeudeId = null;
        var anteilZuGross = Posten(); anteilZuGross.Gesamtbetrag = 5m;
        var ohneBetrag = Posten(); ohneBetrag.Betrag = 0;
        var einnahmeKategorie = Posten(); einnahmeKategorie.KategorieId = _daten.Mieteinnahmen.Id;
        var ohneBeschreibung = Posten(); ohneBeschreibung.Beschreibung = "";

        await FluentActions.Awaiting(() => service.SaveAsync(ohneZuordnung)).Should().ThrowAsync<InvalidOperationException>().WithMessage("*Gebäude oder eine Einheit*");
        await FluentActions.Awaiting(() => service.SaveAsync(anteilZuGross)).Should().ThrowAsync<InvalidOperationException>().WithMessage("*Gesamtbetrag*");
        await FluentActions.Awaiting(() => service.SaveAsync(ohneBetrag)).Should().ThrowAsync<InvalidOperationException>();
        await FluentActions.Awaiting(() => service.SaveAsync(einnahmeKategorie)).Should().ThrowAsync<InvalidOperationException>().WithMessage("*keine Kosten*");
        await FluentActions.Awaiting(() => service.SaveAsync(ohneBeschreibung)).Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Kosten_FremdeigentumIstGesperrt()
    {
        var fremd = TestDaten.Einheit("Whg 5", EinheitStatus.Fremdeigentum);
        _daten.Gebaeude(einheiten: [fremd]);

        var act = () => new KostenService(_daten.Factory).SaveAsync(new Kostenposten
        {
            MieteinheitId = fremd.Id, KategorieId = _daten.Nebenkosten.Id, Betrag = 10m, Zahlungsdatum = new(2026, 1, 1), Beschreibung = "X"
        });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Fremdeigentum*");
    }

    [Fact]
    public async Task Kosten_BearbeitenUndLoeschen()
    {
        var gebaeude = _daten.Gebaeude();
        var service = new KostenService(_daten.Factory);
        var posten = await service.SaveAsync(new Kostenposten
        {
            GebaeudeId = gebaeude.Id, KategorieId = _daten.Nebenkosten.Id, Betrag = 10m, Zahlungsdatum = new(2026, 1, 1), Beschreibung = "Gärtner"
        }, new DateiUpload("a.pdf", "application/pdf", [1]));

        posten.Betrag = 25m;
        await service.SaveAsync(posten, new DateiUpload("b.pdf", "application/pdf", [2]));
        (await service.GetAsync(2026)).Single().Betrag.Should().Be(25m);

        await service.DeleteAsync(posten.Id);
        await service.DeleteAsync(Guid.NewGuid());

        await using var db = _daten.Factory.CreateDbContext();
        (await db.Kostenposten.CountAsync()).Should().Be(0);
        (await db.Dokumente.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Mieteingang_UebernimmtUstSatzDerEinheit()
    {
        var laden = TestDaten.Einheit("Laden", art: EinheitArt.Gewerbe);
        laden.UstOption = true;
        _daten.Gebaeude(einheiten: [laden]);
        var service = new MieteingangService(_daten.Factory);

        await service.SaveAsync(new Mieteingang
        {
            MieteinheitId = laden.Id, Zahlungsdatum = new(2026, 3, 3), Monat = new(2026, 3, 17), Kaltmiete = 1000m
        });

        var eingang = (await service.GetAsync(2026, laden.Id)).Single();
        eingang.UstSatz.Should().Be(19m);
        eingang.Monat.Should().Be(new DateOnly(2026, 3, 1));
        eingang.Brutto.Should().Be(1190m);
    }

    [Fact]
    public async Task Mieteingang_BearbeitenLoeschenUndValidierung()
    {
        var einheit = TestDaten.Einheit("Whg 1");
        _daten.Gebaeude(einheiten: [einheit]);
        var service = new MieteingangService(_daten.Factory);

        var eingang = await service.SaveAsync(new Mieteingang { MieteinheitId = einheit.Id, Zahlungsdatum = new(2026, 3, 3), Monat = new(2026, 3, 1), Kaltmiete = 800m });
        eingang.Kaltmiete = 850m;
        await service.SaveAsync(eingang);
        (await service.GetAsync(2026)).Single().Kaltmiete.Should().Be(850m);

        await service.DeleteAsync(eingang.Id);
        await service.DeleteAsync(Guid.NewGuid());
        (await service.GetAsync(2026)).Should().BeEmpty();

        var ohneBetrag = () => service.SaveAsync(new Mieteingang { MieteinheitId = einheit.Id, Zahlungsdatum = new(2026, 3, 3) });
        var ohneEinheit = () => service.SaveAsync(new Mieteingang { MieteinheitId = Guid.NewGuid(), Kaltmiete = 1m });
        await ohneBetrag.Should().ThrowAsync<InvalidOperationException>();
        await ohneEinheit.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ErfasseMonat_NurVermieteteEigeneEinheitenOhneBestehendenEintrag()
    {
        var vermietet = TestDaten.Einheit("Whg 1");
        var gekuendigt = TestDaten.Einheit("Whg 2", EinheitStatus.Gekuendigt);
        var bereitsErfasst = TestDaten.Einheit("Whg 3");
        var leer = TestDaten.Einheit("Whg 4", EinheitStatus.Leer);
        var fremd = TestDaten.Einheit("Whg 5", EinheitStatus.Fremdeigentum);
        _daten.Gebaeude(einheiten: [vermietet, gekuendigt, bereitsErfasst, leer, fremd]);
        _daten.Speichere(new Mieteingang { Id = Guid.NewGuid(), MieteinheitId = bereitsErfasst.Id, Zahlungsdatum = new(2026, 3, 2), Monat = new(2026, 3, 1), Kaltmiete = 800m });
        var service = new MieteingangService(_daten.Factory);

        var anzahl = await service.ErfasseMonatAsync(new DateOnly(2026, 3, 15), new DateOnly(2026, 3, 3));

        anzahl.Should().Be(2);
        var eingaenge = await service.GetAsync(2026);
        eingaenge.Should().HaveCount(3);
        eingaenge.Where(e => e.MieteinheitId == vermietet.Id).Single().Netto.Should().Be(1000m);
    }

    [Fact]
    public async Task NkAbrechnung_ProJahrNurEinmal_UndErsetztPdf()
    {
        var einheit = TestDaten.Einheit("Whg 1");
        _daten.Gebaeude(einheiten: [einheit]);
        var service = new NebenkostenabrechnungService(_daten.Factory);

        var abrechnung = await service.SaveAsync(
            new Nebenkostenabrechnung { MieteinheitId = einheit.Id, Jahr = 2025, Ergebnis = 120m, Zahlungsdatum = new(2026, 4, 1) },
            new DateiUpload("nk2025.pdf", "application/pdf", [1]));
        abrechnung.Ergebnis = 130m;
        await service.SaveAsync(abrechnung, new DateiUpload("nk2025-korr.pdf", "application/pdf", [2]));

        var doppelt = () => service.SaveAsync(new Nebenkostenabrechnung { MieteinheitId = einheit.Id, Jahr = 2025, Zahlungsdatum = new(2026, 4, 1) });
        await doppelt.Should().ThrowAsync<InvalidOperationException>().WithMessage("*2025*");

        var gespeichert = (await service.GetByEinheitAsync(einheit.Id)).Single();
        gespeichert.Ergebnis.Should().Be(130m);
        await using (var db = _daten.Factory.CreateDbContext())
            (await db.Dokumente.SingleAsync()).Dateiname.Should().Be("nk2025-korr.pdf");

        await service.DeleteAsync(gespeichert.Id);
        await service.DeleteAsync(Guid.NewGuid());
        (await service.GetByEinheitAsync(einheit.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task Darlehen_AnlegenZahlungErfassenUndLoeschen()
    {
        var gebaeude = _daten.Gebaeude();
        var service = new DarlehenService(_daten.Factory);

        var darlehen = await service.SaveAsync(new Darlehen { GebaeudeId = gebaeude.Id, Bezeichnung = " Sparkasse " });
        var zahlung = await service.SaveZahlungAsync(new Darlehenszahlung { DarlehenId = darlehen.Id, Zahlungsdatum = new(2026, 1, 30), Zinsanteil = 300m, Tilgungsanteil = 700m });
        zahlung.Zinsanteil = 310m;
        await service.SaveZahlungAsync(zahlung);

        var geladen = (await service.GetAllAsync()).Single();
        geladen.Bezeichnung.Should().Be("Sparkasse");
        geladen.Zahlungen.Single().Zinsanteil.Should().Be(310m);

        var loeschenMitZahlung = () => service.DeleteAsync(darlehen.Id);
        await loeschenMitZahlung.Should().ThrowAsync<InvalidOperationException>();

        await service.DeleteZahlungAsync(zahlung.Id);
        await service.DeleteZahlungAsync(Guid.NewGuid());
        await service.DeleteAsync(darlehen.Id);
        await service.DeleteAsync(Guid.NewGuid());
        (await service.GetAllAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Darlehen_Validierung()
    {
        var service = new DarlehenService(_daten.Factory);

        await FluentActions.Awaiting(() => service.SaveAsync(new Darlehen())).Should().ThrowAsync<InvalidOperationException>();
        await FluentActions.Awaiting(() => service.SaveAsync(new Darlehen { Bezeichnung = "X", GebaeudeId = Guid.NewGuid() }))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("*Gebäude*");
        await FluentActions.Awaiting(() => service.SaveZahlungAsync(new Darlehenszahlung { Zinsanteil = -1m }))
            .Should().ThrowAsync<InvalidOperationException>();
        await FluentActions.Awaiting(() => service.SaveZahlungAsync(new Darlehenszahlung()))
            .Should().ThrowAsync<InvalidOperationException>();
        await FluentActions.Awaiting(() => service.SaveZahlungAsync(new Darlehenszahlung { DarlehenId = Guid.NewGuid(), Zinsanteil = 1m }))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("Darlehen nicht gefunden.");
    }

    [Fact]
    public async Task Kategorien_NeueNurFuerAusgaben_SystemNichtLoeschbar()
    {
        var service = new KategorieService(_daten.Factory);

        var gaertner = await service.SaveAsync(new Kostenkategorie { Name = " Gärtner ", Typ = KategorieTyp.Werbungskosten, KontoNummer = " " });
        gaertner.Name.Should().Be("Gärtner");
        gaertner.KontoNummer.Should().BeNull();

        var einnahme = () => service.SaveAsync(new Kostenkategorie { Name = "Parkplatz", Typ = KategorieTyp.Mieteinnahme });
        var doppelt = () => service.SaveAsync(new Kostenkategorie { Name = "Gärtner", Typ = KategorieTyp.Werbungskosten });
        var systemLoeschen = () => service.DeleteAsync(_daten.Tilgung.Id);
        var systemTypAendern = () => service.SaveAsync(new Kostenkategorie { Id = _daten.Tilgung.Id, Name = "Tilgung", Typ = KategorieTyp.Werbungskosten });
        var ohneName = () => service.SaveAsync(new Kostenkategorie());

        await einnahme.Should().ThrowAsync<InvalidOperationException>();
        await doppelt.Should().ThrowAsync<InvalidOperationException>();
        await systemLoeschen.Should().ThrowAsync<InvalidOperationException>();
        await systemTypAendern.Should().ThrowAsync<InvalidOperationException>();
        await ohneName.Should().ThrowAsync<InvalidOperationException>();

        (await service.GetAusgabekategorienAsync()).Select(k => k.Typ).Should()
            .NotContain([KategorieTyp.Mieteinnahme, KategorieTyp.Nebenkostenabrechnung]);

        gaertner.KontoNummer = "2350";
        await service.SaveAsync(gaertner);
        await service.DeleteAsync(gaertner.Id);
        await service.DeleteAsync(Guid.NewGuid());
        (await service.GetAllAsync()).Should().NotContain(k => k.Name == "Gärtner");
    }

    [Fact]
    public async Task Kategorien_InVerwendung_NichtLoeschbar()
    {
        var gebaeude = _daten.Gebaeude();
        _daten.Speichere(new Kostenposten { Id = Guid.NewGuid(), GebaeudeId = gebaeude.Id, KategorieId = _daten.Nebenkosten.Id, Betrag = 1m, Zahlungsdatum = new(2026, 1, 1), Beschreibung = "X" });

        var act = () => new KategorieService(_daten.Factory).DeleteAsync(_daten.Nebenkosten.Id);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*verwendet*");
    }
}
