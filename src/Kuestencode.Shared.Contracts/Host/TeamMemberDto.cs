namespace Kuestencode.Shared.Contracts.Host;

/// <summary>
/// Represents a lightweight employee/team member record exposed by the Host API.
/// </summary>
public class TeamMemberDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Role { get; set; }
    public bool IsActive { get; set; }
    public bool IsOffline { get; set; }
    public int? MitarbeiterRolleId { get; set; }
    public string? MitarbeiterRolleName { get; set; }
    public decimal Kostensatz { get; set; }
}
