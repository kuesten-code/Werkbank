using Kuestencode.Werkbank.Locatio.Data;
using Kuestencode.Werkbank.Locatio.Domain;
using Kuestencode.Werkbank.Locatio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Locatio.Services;

public class DarlehenService : IDarlehenService
{
    private readonly IDbContextFactory<LocatioDbContext> _dbFactory;

    public DarlehenService(IDbContextFactory<LocatioDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<Darlehen>> GetAllAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Darlehen.AsNoTracking()
            .Include(d => d.Gebaeude)
            .Include(d => d.Zahlungen.OrderByDescending(z => z.Zahlungsdatum))
            .OrderBy(d => d.Bezeichnung)
            .ToListAsync();
    }

    public async Task<Darlehen> SaveAsync(Darlehen darlehen)
    {
        if (string.IsNullOrWhiteSpace(darlehen.Bezeichnung))
            throw new InvalidOperationException("Bitte eine Bezeichnung für das Darlehen angeben.");

        await using var db = await _dbFactory.CreateDbContextAsync();
        if (!await db.Gebaeude.AnyAsync(g => g.Id == darlehen.GebaeudeId))
            throw new InvalidOperationException("Bitte ein Gebäude zuordnen.");

        var vorhanden = await db.Darlehen.FirstOrDefaultAsync(d => d.Id == darlehen.Id);
        if (vorhanden == null)
        {
            if (darlehen.Id == Guid.Empty)
                darlehen.Id = Guid.NewGuid();
            vorhanden = new Darlehen { Id = darlehen.Id };
            db.Darlehen.Add(vorhanden);
        }

        vorhanden.GebaeudeId = darlehen.GebaeudeId;
        vorhanden.Bezeichnung = darlehen.Bezeichnung.Trim();
        vorhanden.Kreditgeber = darlehen.Kreditgeber;
        vorhanden.Notiz = darlehen.Notiz;

        await db.SaveChangesAsync();
        return darlehen;
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var darlehen = await db.Darlehen.FirstOrDefaultAsync(d => d.Id == id);
        if (darlehen == null)
            return;

        if (await db.Darlehenszahlungen.AnyAsync(z => z.DarlehenId == id))
            throw new InvalidOperationException("Das Darlehen hat Zahlungen und kann nicht gelöscht werden.");

        db.Darlehen.Remove(darlehen);
        await db.SaveChangesAsync();
    }

    public async Task<Darlehenszahlung> SaveZahlungAsync(Darlehenszahlung zahlung)
    {
        if (zahlung.Zinsanteil < 0 || zahlung.Tilgungsanteil < 0)
            throw new InvalidOperationException("Zins und Tilgung dürfen nicht negativ sein.");
        if (zahlung.Zinsanteil + zahlung.Tilgungsanteil == 0)
            throw new InvalidOperationException("Bitte Zins und/oder Tilgung angeben.");

        await using var db = await _dbFactory.CreateDbContextAsync();
        var darlehen = await db.Darlehen.Include(d => d.Gebaeude).FirstOrDefaultAsync(d => d.Id == zahlung.DarlehenId)
            ?? throw new InvalidOperationException("Darlehen nicht gefunden.");
        Buchungssperre.Pruefe(darlehen.Gebaeude!, zahlung.Zahlungsdatum);

        var vorhanden = await db.Darlehenszahlungen.FirstOrDefaultAsync(z => z.Id == zahlung.Id);
        if (vorhanden == null)
        {
            if (zahlung.Id == Guid.Empty)
                zahlung.Id = Guid.NewGuid();
            vorhanden = new Darlehenszahlung { Id = zahlung.Id, DarlehenId = zahlung.DarlehenId };
            db.Darlehenszahlungen.Add(vorhanden);
        }

        vorhanden.Zahlungsdatum = zahlung.Zahlungsdatum;
        vorhanden.Zinsanteil = zahlung.Zinsanteil;
        vorhanden.Tilgungsanteil = zahlung.Tilgungsanteil;
        vorhanden.Notiz = zahlung.Notiz;

        await db.SaveChangesAsync();
        return zahlung;
    }

    public async Task DeleteZahlungAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var zahlung = await db.Darlehenszahlungen.FirstOrDefaultAsync(z => z.Id == id);
        if (zahlung == null)
            return;

        db.Darlehenszahlungen.Remove(zahlung);
        await db.SaveChangesAsync();
    }
}
