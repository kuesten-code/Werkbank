using Kuestencode.Werkbank.Acta.Domain.Entities;
using Kuestencode.Werkbank.Acta.Domain.Enums;

namespace Kuestencode.Werkbank.Acta.Data.Repositories;

/// <summary>
/// Repository für Projekte.
/// </summary>
public interface IProjectRepository
{
    /// <summary>
    /// Lädt ein Projekt mit allen Aufgaben.
    /// </summary>
    Task<Project?> GetByIdAsync(Guid id);

    /// <summary>
    /// Lädt ein Projekt anhand der Projektnummer.
    /// </summary>
    Task<Project?> GetByNumberAsync(string projectNumber);

    /// <summary>
    /// Lädt alle Projekte mit optionalem Filter.
    /// </summary>
    Task<List<Project>> GetAllAsync(ProjectStatus? status = null, int? customerId = null);

    /// <summary>
    /// Lädt Projekte eines Kunden.
    /// </summary>
    Task<List<Project>> GetByCustomerAsync(int customerId);

    /// <summary>
    /// Lädt Projekte mit einem bestimmten Status.
    /// </summary>
    Task<List<Project>> GetByStatusAsync(ProjectStatus status);

    /// <summary>
    /// Fügt ein neues Projekt hinzu.
    /// </summary>
    Task AddAsync(Project project);

    /// <summary>
    /// Aktualisiert ein Projekt.
    /// </summary>
    Task UpdateAsync(Project project);

    /// <summary>
    /// Löscht ein Projekt (nur bei Status Draft erlaubt).
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Prüft, ob eine Projektnummer bereits existiert.
    /// </summary>
    Task<bool> ExistsNumberAsync(string projectNumber);

    /// <summary>
    /// Generiert die nächste verfügbare Projektnummer.
    /// Format: P-YYYY-NNNN (z.B. P-2026-0001)
    /// </summary>
    Task<string> GenerateProjectNumberAsync();

    /// <summary>
    /// Gibt die nächste freie ExternalId zurück.
    /// </summary>
    Task<int> GetNextExternalIdAsync();
}
