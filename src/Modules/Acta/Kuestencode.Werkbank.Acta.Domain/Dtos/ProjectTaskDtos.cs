using Kuestencode.Werkbank.Acta.Domain.Enums;

namespace Kuestencode.Werkbank.Acta.Domain.Dtos;

/// <summary>
/// DTO zum Erstellen einer neuen Aufgabe.
/// </summary>
public class CreateProjectTaskDto
{
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateOnly? TargetDate { get; set; }
    public Guid? AssignedUserId { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// DTO zum Aktualisieren einer Aufgabe.
/// </summary>
public class UpdateProjectTaskDto
{
    public string Title { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateOnly? TargetDate { get; set; }
    public Guid? AssignedUserId { get; set; }
    public ProjectTaskStatus Status { get; set; } = ProjectTaskStatus.Open;
    public int SortOrder { get; set; }
}
