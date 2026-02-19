namespace Kuestencode.Werkbank.Host.Models.MobileRapport;

public class MobileTaskDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public int? ProjectExternalId { get; set; }
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerPostalCode { get; set; }
    public string? CustomerCity { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string? ProjectNumber { get; set; }
    public string? ProjectAddress { get; set; }
    public string? ProjectPostalCode { get; set; }
    public string? ProjectCity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string Status { get; set; } = "Open";
    public DateOnly? TargetDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int SortOrder { get; set; }
}
