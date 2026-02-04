namespace Kuestencode.Werkbank.Acta.Controllers.Dtos;

/// <summary>
/// API-Response DTO für eine Aufgabe.
/// </summary>
public class ProjectTaskDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateOnly? TargetDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? AssignedUserId { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// API-Request DTO zum Erstellen einer Aufgabe.
/// </summary>
public class CreateTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateOnly? TargetDate { get; set; }
    public Guid? AssignedUserId { get; set; }
}

/// <summary>
/// API-Request DTO zum Aktualisieren einer Aufgabe.
/// </summary>
public class UpdateTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateOnly? TargetDate { get; set; }
    public Guid? AssignedUserId { get; set; }
}

/// <summary>
/// API-Request DTO zum Umsortieren von Aufgaben.
/// </summary>
public class ReorderTasksRequest
{
    public List<Guid> TaskIds { get; set; } = new();
}
