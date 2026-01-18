using System.ComponentModel.DataAnnotations;

namespace Kuestencode.Core.Models;

/// <summary>
/// Configuration for SMTP email sending.
/// </summary>
public class SmtpConfiguration
{
    [Required(ErrorMessage = "SMTP Host ist erforderlich")]
    [MaxLength(200)]
    public string Host { get; set; } = string.Empty;

    [Required(ErrorMessage = "SMTP Port ist erforderlich")]
    [Range(1, 65535, ErrorMessage = "Port muss zwischen 1 und 65535 liegen")]
    public int Port { get; set; } = 587;

    public bool UseSsl { get; set; } = true;

    [Required(ErrorMessage = "Benutzername ist erforderlich")]
    [MaxLength(200)]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Passwort ist erforderlich")]
    [MaxLength(500)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Absender-Email ist erforderlich")]
    [EmailAddress(ErrorMessage = "Ungültige Email-Adresse")]
    [MaxLength(200)]
    public string SenderEmail { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? SenderName { get; set; }

    /// <summary>
    /// Creates an SmtpConfiguration from a Company entity.
    /// </summary>
    public static SmtpConfiguration? FromCompany(Company company)
    {
        if (!company.IsEmailConfigured())
            return null;

        return new SmtpConfiguration
        {
            Host = company.SmtpHost!,
            Port = company.SmtpPort!.Value,
            UseSsl = company.SmtpUseSsl,
            Username = company.SmtpUsername!,
            Password = company.SmtpPassword!,
            SenderEmail = company.EmailSenderEmail!,
            SenderName = company.EmailSenderName
        };
    }
}
