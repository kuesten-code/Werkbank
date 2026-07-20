using Kuestencode.Core.Services;
using Kuestencode.Shared.ApiClients;
using Kuestencode.Werkbank.Offerte.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Offerte.Data.Services;

/// <summary>
/// Service zur Generierung eindeutiger Angebotsnummern.
/// Format konfigurierbar über die Host-Nummernkreise (Standard: "ANG-YYYY-XXXXX").
/// </summary>
public class AngebotsnummernService : IAngebotsnummernService
{
    private readonly OfferteDbContext _context;
    private readonly IHostApiClient _hostApiClient;

    public AngebotsnummernService(OfferteDbContext context, IHostApiClient hostApiClient)
    {
        _context = context;
        _hostApiClient = hostApiClient;
    }

    public async Task<string> NaechsteNummerAsync()
    {
        var settings = await _hostApiClient.GetNumberFormatSettingsAsync();
        var format = !string.IsNullOrWhiteSpace(settings?.QuoteFormat)
            ? settings.QuoteFormat.Trim()
            : "ANG-YYYY-XXXXX";

        var existingNumbers = await _context.Angebote
            .Select(a => a.Angebotsnummer)
            .ToListAsync();

        return DocumentNumberFormatter.GenerateNext(format, DateTime.Now, existingNumbers);
    }

    public async Task<bool> ExistiertAsync(string angebotsnummer)
    {
        return await _context.Angebote.AnyAsync(a => a.Angebotsnummer == angebotsnummer);
    }
}
