using System.ComponentModel.DataAnnotations;

namespace Kuestencode.Faktura.Models.CompanyConfiguration;

/// <summary>
/// Email design customization settings.
/// Separate entity with 1:1 relationship to Company.
/// </summary>
public class EmailDesignSettings
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    public EmailLayout Layout { get; set; } = EmailLayout.Klar;

    [MaxLength(7)]
    public string PrimaryColor { get; set; } = "#0F2A3D";

    [MaxLength(7)]
    public string AccentColor { get; set; } = "#3FA796";

    [MaxLength(500)]
    public string? Greeting { get; set; }

    [MaxLength(500)]
    public string? Closing { get; set; }

    // Navigation property
    public Company Company { get; set; } = null!;
}
