using Kuestencode.Faktura.Models;
using MimeKit;

namespace Kuestencode.Faktura.Services.Email;

/// <summary>
/// Interface for building email attachments
/// </summary>
public interface IEmailAttachmentBuilder
{
    /// <summary>
    /// Adds invoice attachments to the body builder based on the specified format
    /// </summary>
    Task AddInvoiceAttachmentsAsync(
        BodyBuilder bodyBuilder,
        int invoiceId,
        string invoiceNumber,
        EmailAttachmentFormat format);
}
