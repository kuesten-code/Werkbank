namespace Kuestencode.Rapport.Models.Timesheets;

/// <summary>
/// Group of entries by project.
/// </summary>
public class TimesheetProjectGroupDto
{
    public int? ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public List<TimesheetEntryDto> Entries { get; set; } = new();
    public decimal SubtotalHours { get; set; }
}
