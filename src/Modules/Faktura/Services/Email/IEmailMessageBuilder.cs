using Kuestencode.Core.Models;
using Kuestencode.Faktura.Models;
using MimeKit;

namespace Kuestencode.Faktura.Services.Email;

/// <summary>
/// Interface for building email messages
/// </summary>
public interface IEmailMessageBuilder
{
    /// <summary>
    /// Builds a complete email message for an invoice
    /// </summary>
    Task<MimeMessage> BuildInvoiceEmailAsync(
        Invoice invoice,
        Company company,
        string recipientEmail,
        string? customMessage,
        EmailAttachmentFormat format,
        string? ccEmails,
        string? bccEmails);
}
