using System.ComponentModel.DataAnnotations;

namespace Kuestencode.Werkbank.Host.Models;

/// <summary>
/// A team member/employee configured in Host and used across modules for task assignments etc.
/// </summary>
public class TeamMember
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Name ist erforderlich")]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Ungültige Email-Adresse")]
    [MaxLength(200)]
    public string? Email { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

