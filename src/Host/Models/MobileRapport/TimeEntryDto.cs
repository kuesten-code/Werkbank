namespace Kuestencode.Werkbank.Host.Models.MobileRapport;

public class TimeEntryDto
{
    public int Id { get; set; }  // Rapport TimeEntry uses int
    public int CustomerId { get; set; }  // Rapport uses CustomerId
    public string ProjectName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public decimal Hours { get; set; }
    public string? Description { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}
