using System.ComponentModel.DataAnnotations;
using Kuestencode.Werkbank.Acta.Domain.Enums;

namespace Kuestencode.Werkbank.Acta.Domain.Entities;

/// <summary>
/// Eine Aufgabe innerhalb eines Projekts.
/// </summary>
public class ProjectTask
{
    public Guid Id { get; set; }

    /// <summary>
    /// Referenz zum übergeordneten Projekt.
    /// </summary>
    [Required]
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Titel der Aufgabe.
    /// </summary>
    [Required(ErrorMessage = "Titel ist erforderlich")]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Optionale Notizen zur Aufgabe.
    /// </summary>
    [MaxLength(2000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Status der Aufgabe.
    /// </summary>
    public ProjectTaskStatus Status { get; set; } = ProjectTaskStatus.Open;

    /// <summary>
    /// Erstellungszeitpunkt.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Geplanter Zieltermin der Aufgabe.
    /// </summary>
    public DateOnly? TargetDate { get; set; }

    /// <summary>
    /// Zeitpunkt der Erledigung.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Optionale Referenz zum zugewiesenen Benutzer im Host-Schema.
    /// </summary>
    public Guid? AssignedUserId { get; set; }

    /// <summary>
    /// Sortierreihenfolge innerhalb des Projekts.
    /// </summary>
    public int SortOrder { get; set; }

    // Navigation Properties
    public Project Project { get; set; } = null!;
}
