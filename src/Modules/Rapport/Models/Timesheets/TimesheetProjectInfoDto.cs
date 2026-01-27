namespace Kuestencode.Rapport.Models.Timesheets;

/// <summary>
/// Project info used in a timesheet when filtered to a single project.
/// </summary>
public class TimesheetProjectInfoDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
}
