using Kuestencode.Werkbank.Offerte.Domain.Entities;

namespace Kuestencode.Werkbank.Offerte.Services;

/// <summary>
/// Service für Offerte-spezifische Einstellungen (E-Mail und PDF Design).
/// </summary>
public interface IOfferteSettingsService
{
    /// <summary>
    /// Lädt die Offerte-Einstellungen. Erstellt einen Default-Eintrag falls keiner existiert.
    /// </summary>
    Task<OfferteSettings> GetSettingsAsync();

    /// <summary>
    /// Speichert die Offerte-Einstellungen.
    /// </summary>
    Task UpdateSettingsAsync(OfferteSettings settings);
}
