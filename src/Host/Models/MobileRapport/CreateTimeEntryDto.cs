namespace Kuestencode.Werkbank.Host.Models.MobileRapport;

public class CreateTimeEntryDto
{
    public int CustomerId { get; set; }  // Rapport uses CustomerId
    public DateOnly Date { get; set; }
    public decimal Hours { get; set; }
    public string? Description { get; set; }
}
