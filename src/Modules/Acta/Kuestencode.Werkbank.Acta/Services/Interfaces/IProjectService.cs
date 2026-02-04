using Kuestencode.Werkbank.Acta.Domain.Dtos;
using Kuestencode.Werkbank.Acta.Domain.Entities;
using Kuestencode.Werkbank.Acta.Domain.Enums;

namespace Kuestencode.Werkbank.Acta.Services;

/// <summary>
/// Service für Projektverwaltung.
/// </summary>
public interface IProjectService
{
    /// <summary>
    /// Lädt alle Projekte mit optionalem Filter.
    /// </summary>
    Task<List<Project>> GetAllAsync(ProjectFilterDto? filter = null);

    /// <summary>
    /// Lädt ein Projekt anhand der ID.
    /// </summary>
    Task<Project?> GetByIdAsync(Guid id);

    /// <summary>
    /// Erstellt ein neues Projekt.
    /// </summary>
    Task<Project> CreateAsync(CreateProjectDto dto);

    /// <summary>
    /// Aktualisiert ein Projekt.
    /// </summary>
    Task<Project> UpdateAsync(Guid id, UpdateProjectDto dto);

    /// <summary>
    /// Ändert den Status eines Projekts unter Verwendung der State Machine.
    /// </summary>
    Task<Project> ChangeStatusAsync(Guid id, ProjectStatus newStatus);

    /// <summary>
    /// Löscht ein Projekt (nur bei Status Draft erlaubt).
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Prüft, ob eine Projektnummer bereits existiert.
    /// </summary>
    Task<bool> ProjectNumberExistsAsync(string projectNumber);

    /// <summary>
    /// Gibt die verfügbaren Statusübergänge für ein Projekt zurück.
    /// </summary>
    IReadOnlyList<(ProjectStatus TargetStatus, string ActionName)> GetAvailableTransitions(Project project);
}
