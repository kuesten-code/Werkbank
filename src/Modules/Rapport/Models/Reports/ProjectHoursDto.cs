namespace Kuestencode.Rapport.Models.Reports;

/// <summary>
/// Aggregated hours for a project.
/// </summary>
public class ProjectHoursDto
{
    /// <summary>
    /// Project identifier.
    /// </summary>
    public int ProjectId { get; set; }

    /// <summary>
    /// Project display name.
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// Total hours for the project.
    /// </summary>
    public decimal Hours { get; set; }
}
