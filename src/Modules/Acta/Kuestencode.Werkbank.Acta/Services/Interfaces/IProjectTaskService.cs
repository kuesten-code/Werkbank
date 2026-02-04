using Kuestencode.Werkbank.Acta.Domain.Dtos;
using Kuestencode.Werkbank.Acta.Domain.Entities;

namespace Kuestencode.Werkbank.Acta.Services;

/// <summary>
/// Service für Projektaufgaben.
/// </summary>
public interface IProjectTaskService
{
    /// <summary>
    /// Lädt alle Aufgaben eines Projekts.
    /// </summary>
    Task<List<ProjectTask>> GetByProjectIdAsync(Guid projectId);

    /// <summary>
    /// Lädt eine Aufgabe anhand der ID.
    /// </summary>
    Task<ProjectTask?> GetByIdAsync(Guid id);

    /// <summary>
    /// Erstellt eine neue Aufgabe für ein Projekt.
    /// </summary>
    Task<ProjectTask> CreateAsync(Guid projectId, CreateProjectTaskDto dto);

    /// <summary>
    /// Aktualisiert eine Aufgabe.
    /// </summary>
    Task<ProjectTask> UpdateAsync(Guid id, UpdateProjectTaskDto dto);

    /// <summary>
    /// Markiert eine Aufgabe als erledigt.
    /// </summary>
    Task<ProjectTask> SetCompletedAsync(Guid id);

    /// <summary>
    /// Setzt eine Aufgabe wieder auf offen.
    /// </summary>
    Task<ProjectTask> SetOpenAsync(Guid id);

    /// <summary>
    /// Ordnet die Aufgaben eines Projekts neu an.
    /// </summary>
    Task ReorderAsync(Guid projectId, List<Guid> taskIds);

    /// <summary>
    /// Löscht eine Aufgabe.
    /// </summary>
    Task DeleteAsync(Guid id);
}
