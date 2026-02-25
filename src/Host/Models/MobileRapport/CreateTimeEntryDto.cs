namespace Kuestencode.Werkbank.Host.Models.MobileRapport;

public class CreateTimeEntryDto
{
    public int? CustomerId { get; set; }
    public int? ProjectId { get; set; }
    public DateOnly Date { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public string? Description { get; set; }
}
