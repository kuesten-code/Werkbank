namespace Kuestencode.Werkbank.Host.Models.MobileRapport;

public class TimeEntryDto
{
    public int Id { get; set; }  // Rapport TimeEntry uses int
    public int CustomerId { get; set; }  // Rapport uses CustomerId
    public int? ProjectId { get; set; }
    public string? CustomerName { get; set; }
    public string? ProjectName { get; set; }
    public DateOnly Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public decimal Hours { get; set; }
    public string? Description { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}
