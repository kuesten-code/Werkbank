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

    // Auth
    public UserRole Role { get; set; } = UserRole.Mitarbeiter;
    public string? PasswordHash { get; set; }

    // Einladung
    public string? InviteToken { get; set; }
    public DateTime? InviteTokenExpires { get; set; }
    public DateTime? InviteAcceptedAt { get; set; }

    // Passwort-Reset
    public string? ResetToken { get; set; }
    public DateTime? ResetTokenExpires { get; set; }

    // Lockout
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockoutUntil { get; set; }
    public bool IsLockedByAdmin { get; set; } = false;

    // Computed
    public bool HasCompletedSetup => InviteAcceptedAt.HasValue;
}

