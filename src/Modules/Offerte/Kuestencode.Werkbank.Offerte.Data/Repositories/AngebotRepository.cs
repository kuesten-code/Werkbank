using Kuestencode.Werkbank.Offerte.Domain.Entities;
using Kuestencode.Werkbank.Offerte.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Offerte.Data.Repositories;

/// <summary>
/// Repository für Angebote mit EF Core.
/// </summary>
public class AngebotRepository : IAngebotRepository
{
    private readonly OfferteDbContext _context;

    public AngebotRepository(OfferteDbContext context)
    {
        _context = context;
    }

    public async Task<Angebot?> GetByIdAsync(Guid id)
    {
        return await _context.Angebote
            .Include(a => a.Positionen.OrderBy(p => p.Position))
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Angebot?> GetByNummerAsync(string angebotsnummer)
    {
        return await _context.Angebote
            .Include(a => a.Positionen.OrderBy(p => p.Position))
            .FirstOrDefaultAsync(a => a.Angebotsnummer == angebotsnummer);
    }

    public async Task<List<Angebot>> GetAllAsync()
    {
        return await _context.Angebote
            .Include(a => a.Positionen.OrderBy(p => p.Position))
            .OrderByDescending(a => a.Erstelldatum)
            .ToListAsync();
    }

    public async Task<List<Angebot>> GetByKundeAsync(int kundeId)
    {
        return await _context.Angebote
            .Include(a => a.Positionen.OrderBy(p => p.Position))
            .Where(a => a.KundeId == kundeId)
            .OrderByDescending(a => a.Erstelldatum)
            .ToListAsync();
    }

    public async Task<List<Angebot>> GetByStatusAsync(AngebotStatus status)
    {
        return await _context.Angebote
            .Include(a => a.Positionen.OrderBy(p => p.Position))
            .Where(a => a.Status == status)
            .OrderByDescending(a => a.Erstelldatum)
            .ToListAsync();
    }

    public async Task<List<Angebot>> GetAbgelaufeneAsync()
    {
        var heute = DateTime.UtcNow.Date;

        return await _context.Angebote
            .Include(a => a.Positionen.OrderBy(p => p.Position))
            .Where(a => a.Status == AngebotStatus.Versendet && a.GueltigBis < heute)
            .OrderBy(a => a.GueltigBis)
            .ToListAsync();
    }

    public async Task AddAsync(Angebot angebot)
    {
        await _context.Angebote.AddAsync(angebot);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Angebot angebot)
    {
        var gespeichertePositionIds = await _context.Angebotspositionen
            .Where(p => p.AngebotId == angebot.Id)
            .Select(p => p.Id)
            .ToListAsync();

        _context.Angebote.Update(angebot);

        // Neue Positionen tragen bereits eine clientseitig erzeugte Guid. EF Core wertet einen gesetzten
        // Schlüssel als "existiert schon" und würde UPDATE statt INSERT ausführen (→ 0 betroffene Zeilen).
        foreach (var position in angebot.Positionen.Where(p => !gespeichertePositionIds.Contains(p.Id)))
        {
            _context.Entry(position).State = EntityState.Added;
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var angebot = await _context.Angebote.FindAsync(id);
        if (angebot == null)
        {
            throw new InvalidOperationException($"Angebot mit ID {id} nicht gefunden.");
        }

        if (angebot.Status != AngebotStatus.Entwurf)
        {
            throw new InvalidOperationException(
                $"Angebot kann nicht gelöscht werden. Nur Angebote im Status 'Entwurf' können gelöscht werden. Aktueller Status: {angebot.Status}");
        }

        _context.Angebote.Remove(angebot);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistiertNummerAsync(string angebotsnummer)
    {
        return await _context.Angebote.AnyAsync(a => a.Angebotsnummer == angebotsnummer);
    }
}
