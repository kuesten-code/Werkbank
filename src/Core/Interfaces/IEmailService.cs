using Kuestencode.Core.Models;

namespace Kuestencode.Core.Interfaces;

/// <summary>
/// Core interface for email sending operations.
/// Module-specific implementations can extend this for their own email needs.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email with optional attachments.
    /// </summary>
    /// <param name="recipientEmail">Primary recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="htmlBody">HTML body content</param>
    /// <param name="plainTextBody">Optional plain text alternative</param>
    /// <param name="attachments">Optional attachments</param>
    /// <param name="ccEmails">Optional CC recipients (comma-separated)</param>
    /// <param name="bccEmails">Optional BCC recipients (comma-separated)</param>
    /// <returns>True if email was sent successfully</returns>
    Task<bool> SendEmailAsync(
        string recipientEmail,
        string subject,
        string htmlBody,
        string? plainTextBody = null,
        IEnumerable<EmailAttachment>? attachments = null,
        string? ccEmails = null,
        string? bccEmails = null);

    /// <summary>
    /// Tests the SMTP connection with current settings.
    /// </summary>
    /// <returns>Success status and optional error message</returns>
    Task<(bool Success, string? ErrorMessage)> TestConnectionAsync();
}

/// <summary>
/// Represents an email attachment.
/// </summary>
public class EmailAttachment
{
    /// <summary>
    /// The file name for the attachment.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// The binary content of the attachment.
    /// </summary>
    public byte[] Content { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// The MIME content type (e.g., "application/pdf", "text/xml").
    /// </summary>
    public string ContentType { get; set; } = "application/octet-stream";
}
