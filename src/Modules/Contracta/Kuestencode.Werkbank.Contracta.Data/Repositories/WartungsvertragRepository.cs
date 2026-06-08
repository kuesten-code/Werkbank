using Kuestencode.Werkbank.Contracta.Domain.Entities;
using Kuestencode.Werkbank.Contracta.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Contracta.Data.Repositories;

public class WartungsvertragRepository : IWartungsvertragRepository
{
    private readonly IDbContextFactory<ContractaDbContext> _contextFactory;

    public WartungsvertragRepository(IDbContextFactory<ContractaDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Wartungsvertrag?> GetByIdAsync(Guid id)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.Wartungsvertraege
            .Include(v => v.Positionen.OrderBy(p => p.Position))
            .Include(v => v.Historien.OrderByDescending(h => h.Abrechnungsdatum))
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<Wartungsvertrag?> GetByNummerAsync(string vertragsnummer)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.Wartungsvertraege
            .Include(v => v.Positionen.OrderBy(p => p.Position))
            .Include(v => v.Historien.OrderByDescending(h => h.Abrechnungsdatum))
            .FirstOrDefaultAsync(v => v.Vertragsnummer == vertragsnummer);
    }

    public async Task<List<Wartungsvertrag>> GetAllAsync()
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.Wartungsvertraege
            .Include(v => v.Positionen.OrderBy(p => p.Position))
            .OrderByDescending(v => v.ErstelltAm)
            .ToListAsync();
    }

    public async Task<List<Wartungsvertrag>> GetByKundeAsync(int kundeId)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.Wartungsvertraege
            .Include(v => v.Positionen.OrderBy(p => p.Position))
            .Where(v => v.KundeId == kundeId)
            .OrderByDescending(v => v.ErstelltAm)
            .ToListAsync();
    }

    public async Task<List<Wartungsvertrag>> GetByStatusAsync(VertragStatus status)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.Wartungsvertraege
            .Include(v => v.Positionen.OrderBy(p => p.Position))
            .Where(v => v.Status == status)
            .OrderByDescending(v => v.ErstelltAm)
            .ToListAsync();
    }

    public async Task<List<Wartungsvertrag>> GetAktiveAsync()
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.Wartungsvertraege
            .Include(v => v.Positionen.OrderBy(p => p.Position))
            .Where(v => v.Status == VertragStatus.Aktiv)
            .OrderBy(v => v.NaechsteAbrechnung)
            .ToListAsync();
    }

    public async Task AddAsync(Wartungsvertrag vertrag)
    {
        await using var context = _contextFactory.CreateDbContext();
        await context.Wartungsvertraege.AddAsync(vertrag);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Wartungsvertrag vertrag)
    {
        await using var context = _contextFactory.CreateDbContext();

        var existing = await context.Wartungsvertraege
            .Include(v => v.Positionen)
            .Include(v => v.Historien)
            .FirstOrDefaultAsync(v => v.Id == vertrag.Id);

        if (existing == null) return;

        context.Entry(existing).CurrentValues.SetValues(vertrag);

        var toDelete = existing.Positionen
            .Where(ep => !vertrag.Positionen.Any(p => p.Id == ep.Id))
            .ToList();
        context.RemoveRange(toDelete);

        foreach (var pos in vertrag.Positionen)
        {
            var existingPos = existing.Positionen.FirstOrDefault(p => p.Id == pos.Id);
            if (existingPos == null)
            {
                pos.WartungsvertragId = vertrag.Id;
                context.Vertragspositionen.Add(pos);
            }
            else
            {
                context.Entry(existingPos).CurrentValues.SetValues(pos);
            }
        }

        foreach (var hist in vertrag.Historien)
        {
            if (!existing.Historien.Any(h => h.Id == hist.Id))
            {
                hist.WartungsvertragId = vertrag.Id;
                context.Abrechnungshistorie.Add(hist);
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var context = _contextFactory.CreateDbContext();
        var vertrag = await context.Wartungsvertraege.FindAsync(id);
        if (vertrag == null)
            throw new InvalidOperationException($"Wartungsvertrag mit ID {id} nicht gefunden.");
        if (vertrag.Status != VertragStatus.Entwurf)
            throw new InvalidOperationException(
                "Nur Wartungsverträge im Status 'Entwurf' können gelöscht werden.");
        context.Wartungsvertraege.Remove(vertrag);
        await context.SaveChangesAsync();
    }

    public async Task<string> GenerateVertragsnummerAsync()
    {
        await using var context = _contextFactory.CreateDbContext();
        var year = DateTime.UtcNow.Year;
        var prefix = $"WV-{year}-";

        var lastNummer = await context.Wartungsvertraege
            .Where(v => v.Vertragsnummer.StartsWith(prefix))
            .OrderByDescending(v => v.Vertragsnummer)
            .Select(v => v.Vertragsnummer)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastNummer != null)
        {
            var numberPart = lastNummer[prefix.Length..];
            if (int.TryParse(numberPart, out var num))
                nextNumber = num + 1;
        }

        return $"{prefix}{nextNumber:D5}";
    }
}
