using System.ComponentModel.DataAnnotations;

namespace Kuestencode.Faktura.Models.CompanyConfiguration;

/// <summary>
/// SMTP configuration for email sending.
/// Separate entity with 1:1 relationship to Company.
/// </summary>
public class SmtpSettings
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    [MaxLength(200)]
    public string? SmtpHost { get; set; }

    public int? SmtpPort { get; set; }

    public bool SmtpUseSsl { get; set; } = true;

    [MaxLength(200)]
    public string? SmtpUsername { get; set; }

    [MaxLength(500)]
    public string? SmtpPassword { get; set; } // Encrypted

    [EmailAddress(ErrorMessage = "Ungültige Email-Adresse")]
    [MaxLength(200)]
    public string? SenderEmail { get; set; }

    [MaxLength(200)]
    public string? SenderName { get; set; }

    [MaxLength(2000)]
    public string? EmailSignature { get; set; }

    // Navigation property
    public Company Company { get; set; } = null!;
}
