using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Kuestencode.Werkbank.Acta.Domain.Enums;

namespace Kuestencode.Werkbank.Acta.Domain.Entities;

/// <summary>
/// Ein Projekt in der Projektverwaltung.
/// </summary>
public class Project
{
    public Guid Id { get; set; }

    /// <summary>
    /// Eindeutige Projektnummer.
    /// </summary>
    [Required(ErrorMessage = "Projektnummer ist erforderlich")]
    [MaxLength(50)]
    public string ProjectNumber { get; set; } = string.Empty;

    /// <summary>
    /// Name des Projekts.
    /// </summary>
    [Required(ErrorMessage = "Projektname ist erforderlich")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optionale Beschreibung des Projekts.
    /// </summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// Referenz zum Kunden im Host-Schema.
    /// </summary>
    [Required]
    public int CustomerId { get; set; }

    /// <summary>
    /// Optionale Projektadresse - Straße.
    /// </summary>
    [MaxLength(200)]
    public string? Address { get; set; }

    /// <summary>
    /// Optionale Projektadresse - Postleitzahl.
    /// </summary>
    [MaxLength(10)]
    public string? PostalCode { get; set; }

    /// <summary>
    /// Optionale Projektadresse - Stadt.
    /// </summary>
    [MaxLength(100)]
    public string? City { get; set; }

    /// <summary>
    /// Aktueller Status des Projekts.
    /// </summary>
    public ProjectStatus Status { get; set; } = ProjectStatus.Draft;

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Geplanter Starttermin des Projekts.
    /// </summary>
    public DateOnly? StartDate { get; set; }

    /// <summary>
    /// Geplanter Zieltermin des Projekts.
    /// </summary>
    public DateOnly? TargetDate { get; set; }

    /// <summary>
    /// Zeitpunkt der Fertigstellung.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Optionales Netto-Budget für das Projekt.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? BudgetNet { get; set; }

    /// <summary>
    /// Externe Projekt-ID für die Verknüpfung mit anderen Modulen (z.B. Rapport).
    /// </summary>
    public int? ExternalId { get; set; }

    // Navigation Properties
    public List<ProjectTask> Tasks { get; set; } = new();

    // Berechnete Eigenschaften

    /// <summary>
    /// Prüft, ob das Projekt bearbeitet werden kann.
    /// </summary>
    [NotMapped]
    public bool IsEditable => Status is ProjectStatus.Draft or ProjectStatus.Active or ProjectStatus.Paused;

    /// <summary>
    /// Prüft, ob das Projekt in einem terminalen Status ist.
    /// </summary>
    [NotMapped]
    public bool IsTerminal => Status is ProjectStatus.Completed or ProjectStatus.Archived;

    /// <summary>
    /// Anzahl der offenen Aufgaben.
    /// </summary>
    [NotMapped]
    public int OpenTasksCount => Tasks.Count(t => t.Status == ProjectTaskStatus.Open);

    /// <summary>
    /// Anzahl der erledigten Aufgaben.
    /// </summary>
    [NotMapped]
    public int CompletedTasksCount => Tasks.Count(t => t.Status == ProjectTaskStatus.Completed);

    /// <summary>
    /// Fortschritt in Prozent (0-100).
    /// </summary>
    [NotMapped]
    public int ProgressPercent => Tasks.Count > 0
        ? (int)Math.Round(100.0 * CompletedTasksCount / Tasks.Count)
        : 0;
}
