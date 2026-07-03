using Kuestencode.Core.Interfaces;
using Kuestencode.Faktura.Models;

namespace Kuestencode.Faktura.Services.Email;

/// <summary>
/// Interface for building email attachments
/// </summary>
public interface IEmailAttachmentBuilder
{
    /// <summary>
    /// Builds the invoice attachments for the specified format
    /// </summary>
    Task<List<EmailAttachment>> BuildInvoiceAttachmentsAsync(
        int invoiceId,
        string invoiceNumber,
        EmailAttachmentFormat format);
}
