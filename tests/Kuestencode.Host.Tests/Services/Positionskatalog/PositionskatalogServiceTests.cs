using FluentAssertions;
using Kuestencode.Shared.Contracts.Host;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Services.Positionskatalog;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Kuestencode.Host.Tests.Services.Positionskatalog;

public sealed class PositionskatalogServiceTests : IDisposable
{
    private static readonly DateTime Januar = new(2026, 1, 15);
    private static readonly DateTime Maerz = new(2026, 3, 15);

    private readonly HostDbContext _db = new(new DbContextOptionsBuilder<HostDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private readonly PositionskatalogService _service;

    public PositionskatalogServiceTests()
    {
        _service = new PositionskatalogService(_db);
    }

    public void Dispose() => _db.Dispose();

    private static PositionsvorlageMerkenDto Position(string text, string? einheit, decimal preis, DateTime datum) => new()
    {
        Text = text,
        Einheit = einheit,
        Einzelpreis = preis,
        Dokumentdatum = datum
    };

    private async Task Merken(params PositionsvorlageMerkenDto[] positionen)
    {
        await _service.MerkenAsync(positionen);
        _db.ChangeTracker.Clear();
    }

    [Fact]
    public async Task Merken_NeuePosition_WirdMitEinheitUndPreisAngelegt()
    {
        await Merken(Position(" Silikonfuge erneuern ", " lfm ", 12.50m, Januar));

        var vorlage = _db.Positionsvorlagen.Single();
        vorlage.Text.Should().Be("Silikonfuge erneuern");
        vorlage.Einheit.Should().Be("lfm");
        vorlage.Einzelpreis.Should().Be(12.50m);
        vorlage.ZuletztVerwendetAm.Should().Be(Januar);
        vorlage.ZuletztVerwendetAm.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task Merken_LeereEinheit_WirdNullGespeichert()
    {
        await Merken(Position("Anfahrt", "  ", 35m, Januar));

        _db.Positionsvorlagen.Single().Einheit.Should().BeNull();
    }

    [Fact]
    public async Task Merken_NeueresDokument_UeberschreibtPreis()
    {
        await Merken(Position("Fliesen verlegen", "m²", 48m, Januar));
        await Merken(Position("fliesen  verlegen", "m²", 52m, Maerz));

        var vorlage = _db.Positionsvorlagen.Single();
        vorlage.Einzelpreis.Should().Be(52m);
        vorlage.Text.Should().Be("fliesen  verlegen");
        vorlage.ZuletztVerwendetAm.Should().Be(Maerz);
    }

    [Fact]
    public async Task Merken_AelteresDokument_UeberschreibtNicht()
    {
        await Merken(Position("Fliesen verlegen", "m²", 52m, Maerz));
        await Merken(Position("Fliesen verlegen", "m²", 48m, Januar));

        _db.Positionsvorlagen.Single().Einzelpreis.Should().Be(52m);
    }

    [Fact]
    public async Task Merken_AndereEinheit_IstEigenerEintrag()
    {
        await Merken(
            Position("Fliesen verlegen", "m²", 48m, Januar),
            Position("Fliesen verlegen", "Pauschal", 900m, Januar));

        _db.Positionsvorlagen.Should().HaveCount(2);
    }

    [Fact]
    public async Task Merken_DublettenImPaket_NeuesteGewinnt()
    {
        await Merken(
            Position("Anfahrt", null, 30m, Januar),
            Position("Anfahrt", null, 35m, Maerz),
            Position("ANFAHRT", null, 32m, Januar));

        var vorlage = _db.Positionsvorlagen.Single();
        vorlage.Einzelpreis.Should().Be(35m);
    }

    [Theory]
    [InlineData("Geselle", "Stunden", 55)]
    [InlineData("Arbeitszeit", "h", 60)]
    [InlineData("", "Stk.", 10)]
    [InlineData("   ", null, 10)]
    [InlineData("Gutschrift", "Stk.", -10)]
    public async Task Merken_NichtKatalogwuerdig_WirdIgnoriert(string text, string? einheit, decimal preis)
    {
        await Merken(Position(text, einheit, preis, Januar));

        _db.Positionsvorlagen.Should().BeEmpty();
    }

    [Fact]
    public async Task Merken_ZuLangerTextOderEinheit_WirdIgnoriert()
    {
        await Merken(
            Position(new string('x', 1001), null, 1m, Januar),
            Position("Kurz", new string('e', 21), 1m, Januar));

        _db.Positionsvorlagen.Should().BeEmpty();
    }

    [Fact]
    public async Task Merken_LeeresPaket_TutNichts()
    {
        await Merken();

        _db.Positionsvorlagen.Should().BeEmpty();
    }

    [Fact]
    public async Task Suche_FindetGemerktePosition()
    {
        await Merken(Position("Silikonfuge erneuern", "lfm", 12.50m, Januar), Position("Anfahrt", null, 35m, Januar));

        var treffer = await _service.SucheAsync("silikn", 10);

        treffer.Should().ContainSingle().Which.Einzelpreis.Should().Be(12.50m);
    }

    [Fact]
    public async Task GetEinheiten_VerschiedeneSortiertOhneLeere()
    {
        await Merken(
            Position("Kabel", "m", 2m, Januar),
            Position("Dose", "Stk.", 3m, Januar),
            Position("Schalter", "Stk.", 8m, Januar),
            Position("Anfahrt", null, 35m, Januar));

        (await _service.GetEinheitenAsync()).Should().Equal("m", "Stk.");
    }

    [Fact]
    public async Task Alle_SortiertNachTextUndEinheit()
    {
        await Merken(
            Position("Kabel", "m", 2m, Januar),
            Position("Anfahrt", null, 35m, Januar),
            Position("Kabel", "Rolle", 80m, Januar));

        (await _service.AlleAsync()).Select(v => $"{v.Text}|{v.Einheit}")
            .Should().Equal("Anfahrt|", "Kabel|m", "Kabel|Rolle");
    }

    [Fact]
    public async Task Speichern_Neu_LegtGetrimmtMitHeutigemDatumAn()
    {
        var ergebnis = await _service.SpeichernAsync(null, " Silikonfuge ", " lfm ", 12m);

        ergebnis.Should().Be(PositionsvorlageSpeichern.Gespeichert);
        var vorlage = _db.Positionsvorlagen.Single();
        vorlage.Text.Should().Be("Silikonfuge");
        vorlage.Einheit.Should().Be("lfm");
        vorlage.Schluessel.Should().Be(PositionsNormalisierer.Schluessel("Silikonfuge", "lfm"));
        vorlage.ZuletztVerwendetAm.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Speichern_Bearbeiten_AendertTextEinheitPreisUndSchluessel()
    {
        await Merken(Position("Fliesn verlegen", "m²", 48m, Januar));
        var id = _db.Positionsvorlagen.Single().Id;

        var ergebnis = await _service.SpeichernAsync(id, "Fliesen verlegen", "m2", 52m);
        _db.ChangeTracker.Clear();

        ergebnis.Should().Be(PositionsvorlageSpeichern.Gespeichert);
        var vorlage = _db.Positionsvorlagen.Single();
        vorlage.Text.Should().Be("Fliesen verlegen");
        vorlage.Einzelpreis.Should().Be(52m);
        vorlage.Schluessel.Should().Be(PositionsNormalisierer.Schluessel("Fliesen verlegen", "m2"));
    }

    [Fact]
    public async Task Speichern_GleicheVorlageUnveraendert_IstKeineDublette()
    {
        await Merken(Position("Anfahrt", null, 35m, Januar));
        var id = _db.Positionsvorlagen.Single().Id;

        (await _service.SpeichernAsync(id, "Anfahrt", null, 40m)).Should().Be(PositionsvorlageSpeichern.Gespeichert);
    }

    [Fact]
    public async Task Speichern_TextUndEinheitEinerAnderenVorlage_IstDublette()
    {
        await Merken(Position("Anfahrt", null, 35m, Januar), Position("Entsorgung", null, 50m, Januar));
        var id = _db.Positionsvorlagen.Single(v => v.Text == "Entsorgung").Id;

        (await _service.SpeichernAsync(null, "ANFAHRT", " ", 1m)).Should().Be(PositionsvorlageSpeichern.Dublette);
        (await _service.SpeichernAsync(id, "anfahrt", null, 1m)).Should().Be(PositionsvorlageSpeichern.Dublette);
    }

    [Theory]
    [InlineData("", "Stk.", 1, PositionsvorlageSpeichern.Ungueltig)]
    [InlineData(null, "Stk.", 1, PositionsvorlageSpeichern.Ungueltig)]
    [InlineData("Dose", "Stk.", -1, PositionsvorlageSpeichern.Ungueltig)]
    [InlineData("Dose", "xxxxxxxxxxxxxxxxxxxxx", 1, PositionsvorlageSpeichern.Ungueltig)]
    [InlineData("Geselle", "Std.", 55, PositionsvorlageSpeichern.Stundeneinheit)]
    public async Task Speichern_UngueltigeEingabe_WirdAbgelehnt(string? text, string einheit, decimal preis, PositionsvorlageSpeichern erwartet)
    {
        (await _service.SpeichernAsync(null, text, einheit, preis)).Should().Be(erwartet);

        _db.Positionsvorlagen.Should().BeEmpty();
    }

    [Fact]
    public async Task Speichern_UnbekannteId_NichtGefunden()
    {
        (await _service.SpeichernAsync(4711, "Anfahrt", null, 1m)).Should().Be(PositionsvorlageSpeichern.NichtGefunden);
    }

    [Fact]
    public async Task Merken_AelteresDokumentNachHandpflege_UeberschreibtNicht()
    {
        await _service.SpeichernAsync(null, "Anfahrt", null, 40m);
        _db.ChangeTracker.Clear();

        await Merken(Position("Anfahrt", null, 35m, Januar));

        _db.Positionsvorlagen.Single().Einzelpreis.Should().Be(40m);
    }

    [Fact]
    public async Task Loeschen_EntferntVorlage_UnbekannteIdTutNichts()
    {
        await Merken(Position("Anfahrt", null, 35m, Januar));
        var id = _db.Positionsvorlagen.Single().Id;

        await _service.LoeschenAsync(4711);
        await _service.LoeschenAsync(id);

        _db.Positionsvorlagen.Should().BeEmpty();
    }
}
