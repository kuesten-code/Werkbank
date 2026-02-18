namespace Kuestencode.Werkbank.Host.Models.MobileRapport;

public class ProjectSelectDto
{
    public int Id { get; set; }  // CustomerId from Customer table
    public string Name { get; set; } = string.Empty;
    public string? ProjectNumber { get; set; }
}
