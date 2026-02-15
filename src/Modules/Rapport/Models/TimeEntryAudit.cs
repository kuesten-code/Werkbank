using System.ComponentModel.DataAnnotations;

namespace Kuestencode.Rapport.Models;

public class TimeEntryAudit
{
    public Guid Id { get; set; }

    public int TimeEntryId { get; set; }

    public Guid ChangedByUserId { get; set; }

    [MaxLength(200)]
    public string? ChangedByUserName { get; set; }

    public DateTime ChangedAt { get; set; }

    [Required]
    [MaxLength(20)]
    public string Action { get; set; } = string.Empty;

    public string? Changes { get; set; }
}
