using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Kuestencode.Core.Models;

namespace Kuestencode.Rapport.Models;

/// <summary>
/// Zeiterfassung für einen Kunden.
/// REGEL: Kunde ist PFLICHT. Projekt ist OPTIONAL.
/// - Wenn Projekt gewählt: CustomerId kommt vom Projekt
/// - Wenn kein Projekt: CustomerId wird direkt gewählt
/// </summary>
public class TimeEntry : BaseEntity
{
    /// <summary>
    /// Start timestamp of the entry.
    /// </summary>
    [Required]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Optional end timestamp of the entry.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Optional description for the entry.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// True if the entry was created manually.
    /// </summary>
    public bool IsManual { get; set; } = false;

    /// <summary>
    /// Required linked customer id.
    /// </summary>
    [Required]
    public int CustomerId { get; set; }

    /// <summary>
    /// Cached customer name for display.
    /// </summary>
    [MaxLength(200)]
    public string? CustomerName { get; set; }

    /// <summary>
    /// Optional customer navigation (host schema).
    /// </summary>
    [ForeignKey(nameof(CustomerId))]
    public Customer? Customer { get; set; }

    /// <summary>
    /// Optional linked project id.
    /// </summary>
    public int? ProjectId { get; set; }

    /// <summary>
    /// Cached project name for display.
    /// </summary>
    [MaxLength(200)]
    public string? ProjectName { get; set; }

    /// <summary>
    /// Current status of the entry.
    /// </summary>
    [Required]
    public TimeEntryStatus Status { get; set; } = TimeEntryStatus.Running;

    /// <summary>
    /// Marks the entry as deleted without removing it from the database.
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Timestamp for soft deletion.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Computed duration of the entry.
    /// </summary>
    [NotMapped]
    public TimeSpan Duration => (EndTime ?? DateTime.UtcNow) - StartTime;
}
