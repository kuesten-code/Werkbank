using Kuestencode.Werkbank.Locatio.Data;
using Kuestencode.Werkbank.Locatio.Domain.Entities;
using Kuestencode.Werkbank.Locatio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Locatio.Tests;

internal sealed class TestDbContextFactory : IDbContextFactory<LocatioDbContext>
{
    private readonly DbContextOptions<LocatioDbContext> _options = new DbContextOptionsBuilder<LocatioDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    public LocatioDbContext CreateDbContext() => new(_options);
}

/// <summary>
/// Testdaten analog zu den Startwerten der Initialmigration.
/// </summary>
internal sealed class TestDaten
{
    public TestDbContextFactory Factory { get; } = new();

    public Kostenkategorie Mieteinnahmen { get; } = Kategorie("Mieteinnahmen", KategorieTyp.Mieteinnahme, "2750", system: true);
    public Kostenkategorie NkAbrechnung { get; } = Kategorie("Nebenkostenabrechnung", KategorieTyp.Nebenkostenabrechnung, "2750", system: true);
    public Kostenkategorie Nebenkosten { get; } = Kategorie("Nebenkosten", KategorieTyp.Werbungskosten, "2350");
    public Kostenkategorie Zinsen { get; } = Kategorie("Darlehenszinsen", KategorieTyp.Darlehenszinsen, "2120", system: true);
    public Kostenkategorie Tilgung { get; } = Kategorie("Tilgung", KategorieTyp.Tilgung, null, system: true);
    public Kostenkategorie Afa { get; } = Kategorie("Abschreibung (AfA)", KategorieTyp.Afa, "4830", system: true);

    public TestDaten()
    {
        using var db = Factory.CreateDbContext();
        db.Kostenkategorien.AddRange(Mieteinnahmen, NkAbrechnung, Nebenkosten, Zinsen, Tilgung, Afa);
        db.SaveChanges();
    }

    public Gebaeude Gebaeude(string name = "Haus am Deich", params Mieteinheit[] einheiten)
    {
        var gebaeude = new Gebaeude { Id = Guid.NewGuid(), Name = name, Einheiten = einheiten.ToList() };
        using var db = Factory.CreateDbContext();
        db.Gebaeude.Add(gebaeude);
        db.SaveChanges();
        return gebaeude;
    }

    public void Speichere(params object[] entities)
    {
        using var db = Factory.CreateDbContext();
        db.AddRange(entities);
        db.SaveChanges();
    }

    public static Mieteinheit Einheit(
        string bezeichnung,
        EinheitStatus status = EinheitStatus.Vermietet,
        EinheitArt art = EinheitArt.Wohnung,
        decimal kaltmiete = 800m,
        decimal nebenkosten = 200m) => new()
    {
        Id = Guid.NewGuid(),
        Bezeichnung = bezeichnung,
        Status = status,
        Art = art,
        Kaltmiete = kaltmiete,
        NebenkostenVorauszahlung = nebenkosten,
        Mieter = status == EinheitStatus.Vermietet ? $"Mieter {bezeichnung}" : null
    };

    private static Kostenkategorie Kategorie(string name, KategorieTyp typ, string? konto, bool system = false) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Typ = typ,
        KontoNummer = konto,
        IstSystem = system
    };
}
