namespace Kuestencode.Rapport.Models.Timesheets;

/// <summary>
/// Customer data used in a timesheet.
/// </summary>
public class TimesheetCustomerInfoDto
{
    public string Name { get; set; } = string.Empty;
    public string? CustomerNumber { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
}
