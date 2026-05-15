namespace Kuestencode.Shared.Contracts.Rapport;

public class ProjectHoursByRolleDto
{
    public int RolleId { get; set; }
    public string RolleName { get; set; } = "";
    public decimal Stunden { get; set; }
}

public class ProjectHoursByTypeResponseDto
{
    public int ProjectId { get; set; }
    public List<ProjectHoursByRolleDto> StundenByRolle { get; set; } = new();
    public List<ProjectHoursByRolleDto> InvoicedStundenByRolle { get; set; } = new();
}
