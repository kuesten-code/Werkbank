using Kuestencode.Werkbank.Acta.Domain.Enums;

namespace Kuestencode.Werkbank.Acta.Controllers.Dtos;

/// <summary>
/// API-Response DTO für ein Projekt.
/// </summary>
public class ProjectDto
{
    public Guid Id { get; set; }
    public string ProjectNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid CustomerId { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? TargetDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public decimal? BudgetNet { get; set; }
    public int OpenTasksCount { get; set; }
    public int CompletedTasksCount { get; set; }
    public int ProgressPercent { get; set; }
    public List<StatusTransitionDto> AvailableTransitions { get; set; } = new();
}

/// <summary>
/// DTO für verfügbare Statusübergänge.
/// </summary>
public class StatusTransitionDto
{
    public string TargetStatus { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
}

/// <summary>
/// API-Request DTO zum Erstellen eines Projekts.
/// </summary>
public class CreateProjectRequest
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
/// API-Request DTO zum Aktualisieren eines Projekts.
/// </summary>
public class UpdateProjectRequest
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
/// API-Request DTO für Statusänderung.
/// </summary>
public class ChangeStatusRequest
{
    public string NewStatus { get; set; } = string.Empty;
}

/// <summary>
/// API-Response DTO für Projektzusammenfassung.
/// </summary>
public class ProjectSummaryDto
{
    public Guid ProjectId { get; set; }
    public string ProjectNumber { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;

    // Budget
    public decimal? BudgetNet { get; set; }

    // Aus Rapport-Modul (falls verfügbar)
    public decimal TotalHours { get; set; }
    public decimal TotalLaborCost { get; set; }

    // Aus Faktura-Modul (falls verfügbar)
    public decimal TotalInvoicedNet { get; set; }
    public int InvoiceCount { get; set; }

    // Berechnet
    public decimal? BudgetRemaining => BudgetNet.HasValue ? BudgetNet.Value - TotalLaborCost : null;
    public decimal Profit => TotalInvoicedNet - TotalLaborCost;
}
