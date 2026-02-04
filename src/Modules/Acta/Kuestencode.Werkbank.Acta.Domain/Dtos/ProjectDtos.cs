using Kuestencode.Werkbank.Acta.Domain.Enums;

namespace Kuestencode.Werkbank.Acta.Domain.Dtos;

/// <summary>
/// DTO zum Erstellen eines neuen Projekts.
/// </summary>
public class CreateProjectDto
{
    public string ProjectNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid CustomerId { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? TargetDate { get; set; }
    public decimal? BudgetNet { get; set; }
}

/// <summary>
/// DTO zum Aktualisieren eines Projekts.
/// </summary>
public class UpdateProjectDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid CustomerId { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? TargetDate { get; set; }
    public decimal? BudgetNet { get; set; }
}

/// <summary>
/// Filter für die Projektabfrage.
/// </summary>
public class ProjectFilterDto
{
    public ProjectStatus? Status { get; set; }
    public Guid? CustomerId { get; set; }
}
