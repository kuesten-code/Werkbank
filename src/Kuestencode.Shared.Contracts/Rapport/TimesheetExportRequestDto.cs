namespace Kuestencode.Shared.Contracts.Rapport;

/// <summary>
/// Request payload for generating a timesheet export.
/// </summary>
public class TimesheetExportRequestDto
{
    public int CustomerId { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int? ProjectId { get; set; }
    public decimal? HourlyRate { get; set; }
    public string? Title { get; set; }
    public string? FileName { get; set; }
    public List<int>? EntryIds { get; set; }
}
