using Kuestencode.Werkbank.Offerte.Data;
using Kuestencode.Werkbank.Offerte.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Offerte.Services;

/// <summary>
/// Service für Offerte-spezifische Einstellungen (E-Mail und PDF Design).
/// </summary>
public class OfferteSettingsService : IOfferteSettingsService
{
    private readonly OfferteDbContext _dbContext;

    public OfferteSettingsService(OfferteDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OfferteSettings> GetSettingsAsync()
    {
        var settings = await _dbContext.Settings.FirstOrDefaultAsync();

        if (settings == null)
        {
            // Erstelle Default-Einstellungen
            settings = new OfferteSettings();
            _dbContext.Settings.Add(settings);
            await _dbContext.SaveChangesAsync();
        }

        return settings;
    }

    public async Task UpdateSettingsAsync(OfferteSettings settings)
    {
        var existing = await _dbContext.Settings.FirstOrDefaultAsync();

        if (existing == null)
        {
            _dbContext.Settings.Add(settings);
        }
        else
        {
            // Update all properties
            existing.EmailLayout = settings.EmailLayout;
            existing.EmailPrimaryColor = settings.EmailPrimaryColor;
            existing.EmailAccentColor = settings.EmailAccentColor;
            existing.EmailGreeting = settings.EmailGreeting;
            existing.EmailClosing = settings.EmailClosing;

            existing.PdfLayout = settings.PdfLayout;
            existing.PdfPrimaryColor = settings.PdfPrimaryColor;
            existing.PdfAccentColor = settings.PdfAccentColor;
            existing.PdfHeaderText = settings.PdfHeaderText;
            existing.PdfFooterText = settings.PdfFooterText;
            existing.PdfValidityNotice = settings.PdfValidityNotice;
        }

        await _dbContext.SaveChangesAsync();
    }
}
