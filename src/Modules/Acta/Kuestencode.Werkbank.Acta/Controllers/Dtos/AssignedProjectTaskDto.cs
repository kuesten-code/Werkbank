namespace Kuestencode.Werkbank.Acta.Controllers.Dtos;

/// <summary>
/// API-Response DTO für mobile "mir zugewiesene Aufgaben".
/// </summary>
public class AssignedProjectTaskDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public int? ProjectExternalId { get; set; }
    public int CustomerId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string? ProjectNumber { get; set; }
    public string? ProjectAddress { get; set; }
    public string? ProjectPostalCode { get; set; }
    public string? ProjectCity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly? TargetDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int SortOrder { get; set; }
}
