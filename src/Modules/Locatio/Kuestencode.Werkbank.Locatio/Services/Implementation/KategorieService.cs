using Kuestencode.Werkbank.Locatio.Data;
using Kuestencode.Werkbank.Locatio.Domain.Entities;
using Kuestencode.Werkbank.Locatio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Locatio.Services;

public class KategorieService : IKategorieService
{
    private readonly IDbContextFactory<LocatioDbContext> _dbFactory;

    public KategorieService(IDbContextFactory<LocatioDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<Kostenkategorie>> GetAllAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Kostenkategorien.AsNoTracking()
            .OrderBy(k => k.Sortierung).ThenBy(k => k.Name)
            .ToListAsync();
    }

    public async Task<List<Kostenkategorie>> GetAusgabekategorienAsync()
    {
        var alle = await GetAllAsync();
        return alle.Where(k => KategorieTypExtensions.Ausgabetypen.Contains(k.Typ)).ToList();
    }

    public async Task<Kostenkategorie> SaveAsync(Kostenkategorie kategorie)
    {
        if (string.IsNullOrWhiteSpace(kategorie.Name))
            throw new InvalidOperationException("Bitte einen Namen für die Kategorie angeben.");

        kategorie.Name = kategorie.Name.Trim();
        kategorie.KontoNummer = string.IsNullOrWhiteSpace(kategorie.KontoNummer) ? null : kategorie.KontoNummer.Trim();

        await using var db = await _dbFactory.CreateDbContextAsync();
        if (await db.Kostenkategorien.AnyAsync(k => k.Name == kategorie.Name && k.Id != kategorie.Id))
            throw new InvalidOperationException($"Eine Kategorie \"{kategorie.Name}\" gibt es bereits.");

        var vorhanden = await db.Kostenkategorien.FirstOrDefaultAsync(k => k.Id == kategorie.Id);
        if (vorhanden == null)
        {
            // Mieteinnahmen und NK-Abrechnung bebucht Locatio über die Systemkategorien.
            if (!KategorieTypExtensions.Ausgabetypen.Contains(kategorie.Typ))
                throw new InvalidOperationException("Neue Kategorien können nur für Ausgaben angelegt werden.");

            if (kategorie.Id == Guid.Empty)
                kategorie.Id = Guid.NewGuid();
            kategorie.IstSystem = false;
            db.Kostenkategorien.Add(kategorie);
        }
        else
        {
            if (vorhanden.IstSystem && vorhanden.Typ != kategorie.Typ)
                throw new InvalidOperationException("Der Typ einer Systemkategorie kann nicht geändert werden.");

            vorhanden.Name = kategorie.Name;
            vorhanden.KontoNummer = kategorie.KontoNummer;
            vorhanden.Sortierung = kategorie.Sortierung;
            vorhanden.Typ = kategorie.Typ;
        }

        await db.SaveChangesAsync();
        return kategorie;
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var kategorie = await db.Kostenkategorien.FirstOrDefaultAsync(k => k.Id == id);
        if (kategorie == null)
            return;

        if (kategorie.IstSystem)
            throw new InvalidOperationException("Systemkategorien können nicht gelöscht werden.");
        if (await db.Kostenposten.AnyAsync(k => k.KategorieId == id))
            throw new InvalidOperationException("Die Kategorie wird von Kostenposten verwendet und kann nicht gelöscht werden.");

        db.Kostenkategorien.Remove(kategorie);
        await db.SaveChangesAsync();
    }
}
