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

    // Mobiler Schnellzugang (für Mitarbeiter-Zeiterfassung)
    [MaxLength(20)]
    public string? MobileToken { get; set; }                    // z.B. "a8f3x9k2m4"
    public string? PinHash { get; set; }                        // 4-stellige PIN, gehashed
    public bool MobilePinSet { get; set; } = false;             // PIN bereits gewählt?
    public int MobilePinFailedAttempts { get; set; } = 0;       // Fehlversuche
    public bool MobileTokenLocked { get; set; } = false;        // Nach 3 Fehlversuchen gesperrt

    // Computed
    public bool HasCompletedSetup => InviteAcceptedAt.HasValue;
}

