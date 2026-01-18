using System.ComponentModel.DataAnnotations;

namespace Kuestencode.Faktura.Models.CompanyConfiguration;

/// <summary>
/// PDF design customization settings.
/// Separate entity with 1:1 relationship to Company.
/// </summary>
public class PdfDesignSettings
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    public PdfLayout Layout { get; set; } = PdfLayout.Klar;

    [MaxLength(7)]
    public string PrimaryColor { get; set; } = "#1f3a5f";

    [MaxLength(7)]
    public string AccentColor { get; set; } = "#3FA796";

    [MaxLength(500)]
    public string? HeaderText { get; set; }

    [MaxLength(1000)]
    public string? FooterText { get; set; }

    [MaxLength(500)]
    public string? PaymentNotice { get; set; }

    // Navigation property
    public Company Company { get; set; } = null!;
}
