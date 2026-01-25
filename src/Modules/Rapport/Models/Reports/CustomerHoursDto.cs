namespace Kuestencode.Rapport.Models.Reports;

/// <summary>
/// Aggregated hours for a customer including optional projects.
/// </summary>
public class CustomerHoursDto
{
    /// <summary>
    /// Customer identifier.
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// Customer display name.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Total hours for the customer.
    /// </summary>
    public decimal TotalHours { get; set; }

    /// <summary>
    /// Project breakdown.
    /// </summary>
    public List<ProjectHoursDto> Projects { get; set; } = new();

    /// <summary>
    /// Hours without a project.
    /// </summary>
    public decimal EntriesWithoutProject { get; set; }
}
