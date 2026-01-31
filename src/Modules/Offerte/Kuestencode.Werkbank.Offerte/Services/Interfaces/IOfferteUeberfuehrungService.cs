using Kuestencode.Shared.Contracts.Offerte;

namespace Kuestencode.Werkbank.Offerte.Services;

/// <summary>
/// Service zur Überführung von Angeboten in Rechnungen.
/// </summary>
public interface IOfferteUeberfuehrungService
{
    /// <summary>
    /// Erstellt ein DTO zur Überführung des Angebots in eine Rechnung.
    /// Das DTO wird vom Faktura-Modul zur Rechnungserstellung verwendet.
    /// </summary>
    /// <param name="angebotId">ID des Angebots.</param>
    /// <returns>DTO mit den Angebotsdaten für die Rechnungserstellung.</returns>
    Task<RechnungErstellungDto> InRechnungUeberfuehrenAsync(Guid angebotId);
}
