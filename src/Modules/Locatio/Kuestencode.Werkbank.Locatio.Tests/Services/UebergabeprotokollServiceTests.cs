using FluentAssertions;
using Kuestencode.Werkbank.Locatio.Domain.Dtos;
using Kuestencode.Werkbank.Locatio.Domain.Entities;
using Kuestencode.Werkbank.Locatio.Domain.Enums;
using Kuestencode.Werkbank.Locatio.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Kuestencode.Werkbank.Locatio.Tests.Services;

public class UebergabeprotokollServiceTests
{
    private readonly TestDaten _daten = new();
    private readonly UebergabeprotokollService _service;
    private readonly Mieteinheit _einheit = TestDaten.Einheit("Whg 1");

    public UebergabeprotokollServiceTests()
    {
        _service = new UebergabeprotokollService(_daten.Factory);
        _daten.Gebaeude(einheiten: [_einheit]);
    }

    private static DateiUpload Pdf(string name = "protokoll.pdf") => new(name, "application/pdf", [1, 2, 3]);

    private Uebergabeprotokoll Protokoll(UebergabeArt art, DateOnly datum) => new()
    {
        MieteinheitId = _einheit.Id,
        Art = art,
        Datum = datum,
        Mieter = "  Hans Hansen  "
    };

    [Fact]
    public async Task Save_Neu_LegtProtokollUndDokumentAn()
    {
        var gespeichert = await _service.SaveAsync(Protokoll(UebergabeArt.Einzug, new DateOnly(2026, 3, 1)), Pdf());

        await using var db = _daten.Factory.CreateDbContext();
        var protokoll = await db.Uebergabeprotokolle.Include(p => p.Dokument).SingleAsync();
        protokoll.Id.Should().Be(gespeichert.Id);
        protokoll.Mieter.Should().Be("Hans Hansen");
        protokoll.Dokument!.Dateiname.Should().Be("protokoll.pdf");
        gespeichert.DokumentId.Should().Be(protokoll.DokumentId);
    }

    [Fact]
    public async Task Save_NeuOhneDatei_WirftException()
    {
        var act = () => _service.SaveAsync(Protokoll(UebergabeArt.Einzug, new DateOnly(2026, 3, 1)), null);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Datei*");
    }

    [Fact]
    public async Task Save_UnbekannteEinheit_WirftException()
    {
        var protokoll = Protokoll(UebergabeArt.Einzug, new DateOnly(2026, 3, 1));
        protokoll.MieteinheitId = Guid.NewGuid();

        var act = () => _service.SaveAsync(protokoll, Pdf());

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*nicht gefunden*");
    }

    [Fact]
    public async Task Save_Bearbeiten_OhneDateiBehaeltDokument_MitDateiErsetztEs()
    {
        var protokoll = await _service.SaveAsync(Protokoll(UebergabeArt.Einzug, new DateOnly(2026, 3, 1)), Pdf("alt.pdf"));
        var altesDokument = protokoll.DokumentId;

        protokoll.Art = UebergabeArt.Auszug;
        protokoll.Mieter = " ";
        await _service.SaveAsync(protokoll, null);
        (await _service.GetByEinheitAsync(_einheit.Id)).Single().DokumentId.Should().Be(altesDokument);

        await _service.SaveAsync(protokoll, Pdf("neu.pdf"));

        await using var db = _daten.Factory.CreateDbContext();
        var gespeichert = await db.Uebergabeprotokolle.Include(p => p.Dokument).SingleAsync();
        gespeichert.Art.Should().Be(UebergabeArt.Auszug);
        gespeichert.Mieter.Should().BeNull();
        gespeichert.Dokument!.Dateiname.Should().Be("neu.pdf");
        (await db.Dokumente.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task GetByEinheit_NeuesteZuerst()
    {
        await _service.SaveAsync(Protokoll(UebergabeArt.Einzug, new DateOnly(2020, 3, 1)), Pdf());
        await _service.SaveAsync(Protokoll(UebergabeArt.Auszug, new DateOnly(2026, 2, 28)), Pdf());

        var protokolle = await _service.GetByEinheitAsync(_einheit.Id);

        protokolle.Select(p => p.Art).Should().Equal(UebergabeArt.Auszug, UebergabeArt.Einzug);
    }

    [Fact]
    public async Task Delete_EntferntProtokollUndDokument_UnbekanntIgnoriert()
    {
        var protokoll = await _service.SaveAsync(Protokoll(UebergabeArt.Einzug, new DateOnly(2026, 3, 1)), Pdf());

        await _service.DeleteAsync(protokoll.Id);
        await _service.DeleteAsync(Guid.NewGuid());

        await using var db = _daten.Factory.CreateDbContext();
        (await db.Uebergabeprotokolle.AnyAsync()).Should().BeFalse();
        (await db.Dokumente.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task EinheitLoeschen_EntferntProtokolleMit()
    {
        await _service.SaveAsync(Protokoll(UebergabeArt.Einzug, new DateOnly(2026, 3, 1)), Pdf());

        await new EinheitService(_daten.Factory).DeleteAsync(_einheit.Id);

        await using var db = _daten.Factory.CreateDbContext();
        (await db.Uebergabeprotokolle.AnyAsync()).Should().BeFalse();
        (await db.Dokumente.AnyAsync()).Should().BeFalse();
        (await db.Mieteinheiten.AnyAsync()).Should().BeFalse();
    }
}
