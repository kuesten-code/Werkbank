namespace Kuestencode.Shared.Contracts.Host;

/// <summary>
/// Represents a lightweight employee/team member record exposed by the Host API.
/// </summary>
public class TeamMemberDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
}
