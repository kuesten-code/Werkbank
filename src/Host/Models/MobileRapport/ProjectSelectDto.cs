namespace Kuestencode.Werkbank.Host.Models.MobileRapport;

public class ProjectSelectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ProjectNumber { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
}
