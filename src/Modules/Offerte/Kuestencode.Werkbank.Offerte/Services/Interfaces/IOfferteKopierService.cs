using Kuestencode.Werkbank.Offerte.Domain.Entities;

namespace Kuestencode.Werkbank.Offerte.Services;

/// <summary>
/// Service zum Kopieren von Angeboten.
/// </summary>
public interface IOfferteKopierService
{
    /// <summary>
    /// Erstellt eine Kopie eines bestehenden Angebots.
    /// Die Kopie erhält eine neue Nummer, neues Erstelldatum und Status Entwurf.
    /// </summary>
    /// <param name="angebotId">ID des zu kopierenden Angebots.</param>
    /// <param name="gueltigkeitsTage">Gültigkeitsdauer der Kopie in Tagen (Standard: 14).</param>
    /// <returns>Das neu erstellte Angebot.</returns>
    Task<Angebot> KopiereAsync(Guid angebotId, int gueltigkeitsTage = 14);
}
