using FluentAssertions;
using Kuestencode.Werkbank.Locatio.Domain.Dtos;
using Kuestencode.Werkbank.Locatio.Domain.Entities;
using Kuestencode.Werkbank.Locatio.Domain.Enums;
using Kuestencode.Werkbank.Locatio.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Kuestencode.Werkbank.Locatio.Tests.Services;

public class EinheitServiceTests
{
    private readonly TestDaten _daten = new();
    private readonly EinheitService _service;

    public EinheitServiceTests()
    {
        _service = new EinheitService(_daten.Factory);
    }

    [Fact]
    public async Task Save_Fremdeigentum_EntferntEigentumsUndMietdaten()
    {
        var gebaeude = _daten.Gebaeude();
        var einheit = TestDaten.Einheit("Whg 5", EinheitStatus.Fremdeigentum);
        einheit.GebaeudeId = gebaeude.Id;
        einheit.AnschaffungskostenGebaeude = 100_000m;

        await _service.SaveAsync(einheit);

        var gespeichert = await _service.GetAsync(einheit.Id);
        gespeichert!.AnschaffungskostenGebaeude.Should().BeNull();
        gespeichert.Kaltmiete.Should().Be(0m);
        gespeichert.Mieter.Should().BeNull();
        gespeichert.Gebaeude!.Name.Should().Be(gebaeude.Name);
    }

    [Fact]
    public async Task Save_FremdeigentumMitBuchungen_WirftException()
    {
        var einheit = TestDaten.Einheit("Whg 1");
        _daten.Gebaeude(einheiten: [einheit]);
        _daten.Speichere(new Mieteingang { Id = Guid.NewGuid(), MieteinheitId = einheit.Id, Zahlungsdatum = new(2026, 1, 1), Monat = new(2026, 1, 1), Kaltmiete = 1m });

        einheit.Status = EinheitStatus.Fremdeigentum;
        var act = () => _service.SaveAsync(einheit);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Buchungen*");
    }

    [Fact]
    public async Task Save_WohnungMitUstOption_SetztOptionZurueck()
    {
        var gebaeude = _daten.Gebaeude();
        var einheit = TestDaten.Einheit("Whg 1");
        einheit.GebaeudeId = gebaeude.Id;
        einheit.UstOption = true;

        await _service.SaveAsync(einheit);

        (await _service.GetAsync(einheit.Id))!.UstOption.Should().BeFalse();
    }

    [Fact]
    public async Task Save_StatusVeraeussertDirekt_WirftException()
    {
        var einheit = TestDaten.Einheit("Whg 1");
        _daten.Gebaeude(einheiten: [einheit]);

        einheit.Status = EinheitStatus.Veraeussert;
        var act = () => _service.SaveAsync(einheit);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*veräußern*");
    }

    [Fact]
    public async Task Save_Validierung()
    {
        var ohneBezeichnung = () => _service.SaveAsync(new Mieteinheit());
        var ohneGebaeude = () => _service.SaveAsync(new Mieteinheit { Bezeichnung = "X", GebaeudeId = Guid.NewGuid() });
        var vertragVerdreht = () => _service.SaveAsync(new Mieteinheit
        {
            Bezeichnung = "X", VertragsBeginn = new DateOnly(2026, 5, 1), VertragsEnde = new DateOnly(2026, 4, 1)
        });

        await ohneBezeichnung.Should().ThrowAsync<InvalidOperationException>();
        await ohneGebaeude.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Gebäude*");
        await vertragVerdreht.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Vertragsende*");
    }

    [Fact]
    public async Task Save_AktualisiertVorhandeneEinheit()
    {
        var einheit = TestDaten.Einheit("Whg 1");
        _daten.Gebaeude(einheiten: [einheit]);

        einheit.Kaltmiete = 950m;
        einheit.Miteigentumsanteil = "125/1000";
        await _service.SaveAsync(einheit);

        var gespeichert = await _service.GetAsync(einheit.Id);
        gespeichert!.Kaltmiete.Should().Be(950m);
        gespeichert.Miteigentumsanteil.Should().Be("125/1000");
    }

    [Fact]
    public async Task SetGrundriss_ErsetztVorhandenenGrundriss()
    {
        var einheit = TestDaten.Einheit("Whg 1");
        _daten.Gebaeude(einheiten: [einheit]);

        await _service.SetGrundrissAsync(einheit.Id, new DateiUpload("alt.pdf", "application/pdf", [1]));
        await _service.SetGrundrissAsync(einheit.Id, new DateiUpload("neu.png", "image/png", [2, 3]));

        await using var db = _daten.Factory.CreateDbContext();
        var dokument = await db.Dokumente.SingleAsync();
        dokument.Dateiname.Should().Be("neu.png");
        (await db.Mieteinheiten.SingleAsync()).GrundrissId.Should().Be(dokument.Id);
    }

    [Fact]
    public async Task Delete_OhneBuchungenLoescht_MitBuchungenWirftException()
    {
        var frei = TestDaten.Einheit("Whg 1");
        var gebucht = TestDaten.Einheit("Whg 2");
        _daten.Gebaeude(einheiten: [frei, gebucht]);
        _daten.Speichere(new Nebenkostenabrechnung { Id = Guid.NewGuid(), MieteinheitId = gebucht.Id, Jahr = 2025, Ergebnis = 1m, Zahlungsdatum = new(2026, 1, 1) });

        await _service.DeleteAsync(frei.Id);
        await _service.DeleteAsync(Guid.NewGuid());
        var act = () => _service.DeleteAsync(gebucht.Id);

        await act.Should().ThrowAsync<InvalidOperationException>();
        (await _service.GetAsync(frei.Id)).Should().BeNull();
    }
}
