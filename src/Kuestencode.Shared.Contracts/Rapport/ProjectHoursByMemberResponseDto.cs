namespace Kuestencode.Shared.Contracts.Rapport;

public class ProjectHoursByMemberDto
{
    public Guid TeamMemberId { get; set; }
    public string TeamMemberName { get; set; } = "";
    public decimal Stunden { get; set; }
}

public class ProjectHoursByMemberResponseDto
{
    public int ProjectId { get; set; }
    public List<ProjectHoursByMemberDto> StundenByMitarbeiter { get; set; } = new();
}
