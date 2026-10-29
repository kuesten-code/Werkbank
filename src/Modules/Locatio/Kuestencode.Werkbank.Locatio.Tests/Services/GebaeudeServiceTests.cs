using FluentAssertions;
using Kuestencode.Werkbank.Locatio.Domain.Entities;
using Kuestencode.Werkbank.Locatio.Domain.Enums;
using Kuestencode.Werkbank.Locatio.Services;
using Xunit;

namespace Kuestencode.Werkbank.Locatio.Tests.Services;

public class GebaeudeServiceTests
{
    private readonly TestDaten _daten = new();
    private readonly GebaeudeService _service;

    public GebaeudeServiceTests()
    {
        _service = new GebaeudeService(_daten.Factory, new SaldoBuchungService(_daten.Factory));
    }

    [Fact]
    public async Task Kachel_SechsEinheitenDavonZweiFremdeigentum_ZeigtVierVonSechsDreiVermietet()
    {
        var gebaeude = await _service.SaveAsync(new Gebaeude { Name = "Haus am Deich", Ort = "Husum" });
        var einheiten = new EinheitService(_daten.Factory);
        await einheiten.SaveAsync(Neu(gebaeude.Id, "Whg 1", EinheitStatus.Vermietet));
        await einheiten.SaveAsync(Neu(gebaeude.Id, "Whg 2", EinheitStatus.Vermietet));
        await einheiten.SaveAsync(Neu(gebaeude.Id, "Whg 3", EinheitStatus.Gekuendigt));
        await einheiten.SaveAsync(Neu(gebaeude.Id, "Whg 4", EinheitStatus.Leer));
        await einheiten.SaveAsync(Neu(gebaeude.Id, "Whg 5", EinheitStatus.Fremdeigentum));
        await einheiten.SaveAsync(Neu(gebaeude.Id, "Whg 6", EinheitStatus.Fremdeigentum));

        var kachel = (await _service.GetKachelnAsync(2026)).Should().ContainSingle().Subject;

        kachel.EinheitenGesamt.Should().Be(6);
        kachel.EinheitenEigentum.Should().Be(4);
        kachel.EinheitenVermietet.Should().Be(3);
        kachel.EinheitenLeer.Should().Be(1);
        kachel.SollmieteMonat.Should().Be(3_000m);
        kachel.IstVeraeussert.Should().BeFalse();
        kachel.Adresse.Should().Be("Husum");
    }

    [Fact]
    public async Task Kachel_SummiertEinnahmenUndWerbungskostenDesJahres()
    {
        var einheit = TestDaten.Einheit("Whg 1");
        var gebaeude = _daten.Gebaeude(einheiten: [einheit]);
        _daten.Speichere(
            new Mieteingang { Id = Guid.NewGuid(), MieteinheitId = einheit.Id, Zahlungsdatum = new(2026, 2, 1), Monat = new(2026, 2, 1), Kaltmiete = 800m, Nebenkosten = 200m },
            new Kostenposten { Id = Guid.NewGuid(), GebaeudeId = gebaeude.Id, KategorieId = _daten.Nebenkosten.Id, Betrag = 300m, Zahlungsdatum = new(2026, 2, 5), Beschreibung = "Wasser" },
            new Kostenposten { Id = Guid.NewGuid(), GebaeudeId = gebaeude.Id, KategorieId = _daten.Tilgung.Id, Betrag = 5_000m, Zahlungsdatum = new(2026, 2, 5), Beschreibung = "Tilgung" });

        var kachel = (await _service.GetKachelnAsync(2026)).Single();

        kachel.EinnahmenJahr.Should().Be(1_000m);
        kachel.WerbungskostenJahr.Should().Be(300m);
        kachel.UeberschussJahr.Should().Be(700m);
    }

    [Fact]
    public async Task Save_AktualisiertVorhandenesGebaeude()
    {
        var gebaeude = await _service.SaveAsync(new Gebaeude { Name = "  Altbau  " });
        gebaeude.Name.Should().Be("Altbau");

        gebaeude.AbrechnerUrl = "https://kalo.de";
        await _service.SaveAsync(gebaeude);

        (await _service.GetAsync(gebaeude.Id))!.AbrechnerUrl.Should().Be("https://kalo.de");
    }

    [Fact]
    public async Task Save_OhneName_WirftException()
    {
        var act = () => _service.SaveAsync(new Gebaeude());

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Save_VerkaufsdatumDirektSetzen_WirftException()
    {
        var neu = () => _service.SaveAsync(new Gebaeude { Name = "X", VerkaufsDatum = new DateOnly(2026, 1, 1) });
        await neu.Should().ThrowAsync<InvalidOperationException>().WithMessage("*veräußern*");

        var gebaeude = await _service.SaveAsync(new Gebaeude { Name = "Y" });
        gebaeude.VerkaufsDatum = new DateOnly(2026, 1, 1);
        var bestehend = () => _service.SaveAsync(gebaeude);
        await bestehend.Should().ThrowAsync<InvalidOperationException>().WithMessage("*veräußern*");
    }

    [Fact]
    public async Task Delete_MitEinheiten_WirftException_OhneEinheiten_Loescht()
    {
        var mitEinheit = _daten.Gebaeude("Mit", TestDaten.Einheit("Whg 1"));
        var leer = _daten.Gebaeude("Leer");

        var act = () => _service.DeleteAsync(mitEinheit.Id);
        await act.Should().ThrowAsync<InvalidOperationException>();

        await _service.DeleteAsync(leer.Id);
        await _service.DeleteAsync(Guid.NewGuid());
        (await _service.GetAllAsync()).Should().ContainSingle().Which.Name.Should().Be("Mit");
    }

    private static Mieteinheit Neu(Guid gebaeudeId, string bezeichnung, EinheitStatus status)
    {
        var einheit = TestDaten.Einheit(bezeichnung, status);
        einheit.Id = Guid.Empty;
        einheit.GebaeudeId = gebaeudeId;
        return einheit;
    }
}
