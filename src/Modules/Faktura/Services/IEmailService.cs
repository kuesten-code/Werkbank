using Kuestencode.Core.Models;
using Kuestencode.Faktura.Models;

namespace Kuestencode.Faktura.Services;

public interface IEmailService
{
    Task<bool> SendInvoiceEmailAsync(
        int invoiceId,
        string recipientEmail,
        string? customMessage = null,
        EmailAttachmentFormat format = EmailAttachmentFormat.NormalPdf,
        string? ccEmails = null,
        string? bccEmails = null,
        bool includeClosing = true);
    Task<(bool success, string? errorMessage)> TestEmailConnectionAsync(Company company);
}
