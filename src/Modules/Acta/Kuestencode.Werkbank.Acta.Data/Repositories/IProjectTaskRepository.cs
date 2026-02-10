using Kuestencode.Werkbank.Acta.Domain.Entities;

namespace Kuestencode.Werkbank.Acta.Data.Repositories;

/// <summary>
/// Repository für Projektaufgaben.
/// </summary>
public interface IProjectTaskRepository
{
    /// <summary>
    /// Lädt eine Aufgabe anhand der ID.
    /// </summary>
    Task<ProjectTask?> GetByIdAsync(Guid id);

    /// <summary>
    /// Lädt alle Aufgaben eines Projekts, sortiert nach SortOrder.
    /// </summary>
    Task<List<ProjectTask>> GetByProjectIdAsync(Guid projectId);

    /// <summary>
    /// Fügt eine neue Aufgabe hinzu.
    /// </summary>
    Task AddAsync(ProjectTask task);

    /// <summary>
    /// Aktualisiert eine Aufgabe.
    /// </summary>
    Task UpdateAsync(ProjectTask task);

    /// <summary>
    /// Aktualisiert mehrere Aufgaben (für Reordering).
    /// </summary>
    Task UpdateRangeAsync(IEnumerable<ProjectTask> tasks);

    /// <summary>
    /// Löscht eine Aufgabe.
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Ermittelt den nächsten SortOrder-Wert für ein Projekt.
    /// </summary>
    Task<int> GetNextSortOrderAsync(Guid projectId);
}
