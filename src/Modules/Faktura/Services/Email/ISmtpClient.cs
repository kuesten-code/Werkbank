using Kuestencode.Core.Models;
using MimeKit;

namespace Kuestencode.Faktura.Services.Email;

/// <summary>
/// Interface for SMTP client operations
/// </summary>
public interface ISmtpClient
{
    /// <summary>
    /// Sends an email message using SMTP
    /// </summary>
    Task SendAsync(MimeMessage message, Company company);

    /// <summary>
    /// Tests the SMTP connection with the given company settings
    /// </summary>
    Task<(bool success, string? errorMessage)> TestConnectionAsync(Company company);
}
