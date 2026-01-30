using Kuestencode.Werkbank.Offerte.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Offerte.Data.Services;

/// <summary>
/// Service zur Generierung eindeutiger Angebotsnummern.
/// Format: "ANG-2025-00001" (ANG-Jahr-LaufendeNummer)
/// </summary>
public class AngebotsnummernService : IAngebotsnummernService
{
    private readonly OfferteDbContext _context;
    private const string Prefix = "ANG";

    public AngebotsnummernService(OfferteDbContext context)
    {
        _context = context;
    }

    public async Task<string> NaechsteNummerAsync()
    {
        var currentYear = DateTime.UtcNow.Year;
        var yearPrefix = $"{Prefix}-{currentYear}";

        // Finde die höchste Nummer im aktuellen Jahr
        var lastAngebot = await _context.Angebote
            .Where(a => a.Angebotsnummer.StartsWith(yearPrefix))
            .OrderByDescending(a => a.Angebotsnummer)
            .FirstOrDefaultAsync();

        if (lastAngebot == null)
        {
            return $"{yearPrefix}-00001";
        }

        // Extrahiere die Nummer aus der letzten Angebotsnummer
        // Format: "ANG-2025-00001"
        var parts = lastAngebot.Angebotsnummer.Split('-');
        if (parts.Length >= 3 && int.TryParse(parts[^1], out int lastNumber))
        {
            var nextNumber = lastNumber + 1;
            return $"{yearPrefix}-{nextNumber:D5}";
        }

        // Fallback falls Parsing fehlschlägt
        return $"{yearPrefix}-00001";
    }

    public async Task<bool> ExistiertAsync(string angebotsnummer)
    {
        return await _context.Angebote.AnyAsync(a => a.Angebotsnummer == angebotsnummer);
    }
}
