using Kuestencode.Werkbank.Offerte.Domain.Entities;
using Kuestencode.Werkbank.Offerte.Domain.Enums;

namespace Kuestencode.Werkbank.Offerte.Data.Repositories;

/// <summary>
/// Repository für Angebote.
/// </summary>
public interface IAngebotRepository
{
    /// <summary>
    /// Lädt ein Angebot mit allen Positionen.
    /// </summary>
    Task<Angebot?> GetByIdAsync(Guid id);

    /// <summary>
    /// Lädt ein Angebot anhand der Angebotsnummer.
    /// </summary>
    Task<Angebot?> GetByNummerAsync(string angebotsnummer);

    /// <summary>
    /// Lädt alle Angebote mit Positionen, sortiert nach Erstelldatum (neueste zuerst).
    /// </summary>
    Task<List<Angebot>> GetAllAsync();

    /// <summary>
    /// Lädt alle Angebote eines Kunden.
    /// </summary>
    Task<List<Angebot>> GetByKundeAsync(int kundeId);

    /// <summary>
    /// Lädt alle Angebote mit einem bestimmten Status.
    /// </summary>
    Task<List<Angebot>> GetByStatusAsync(AngebotStatus status);

    /// <summary>
    /// Lädt alle versendeten Angebote, die abgelaufen sind.
    /// </summary>
    Task<List<Angebot>> GetAbgelaufeneAsync();

    /// <summary>
    /// Fügt ein neues Angebot hinzu.
    /// </summary>
    Task AddAsync(Angebot angebot);

    /// <summary>
    /// Aktualisiert ein bestehendes Angebot.
    /// </summary>
    Task UpdateAsync(Angebot angebot);

    /// <summary>
    /// Löscht ein Angebot. Nur erlaubt bei Status Entwurf!
    /// </summary>
    /// <exception cref="InvalidOperationException">Wenn das Angebot nicht im Entwurf-Status ist.</exception>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Prüft, ob eine Angebotsnummer bereits existiert.
    /// </summary>
    Task<bool> ExistiertNummerAsync(string angebotsnummer);
}
