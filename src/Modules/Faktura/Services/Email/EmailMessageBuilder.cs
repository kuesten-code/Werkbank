using Kuestencode.Core.Models;
using Kuestencode.Faktura.Models;

namespace Kuestencode.Faktura.Services.Email;

/// <summary>
/// Builds complete email messages with content and attachments
/// </summary>
public class EmailMessageBuilder : IEmailMessageBuilder
{
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly IEmailAttachmentBuilder _attachmentBuilder;

    public EmailMessageBuilder(
        IEmailTemplateRenderer templateRenderer,
        IEmailAttachmentBuilder attachmentBuilder)
    {
        _templateRenderer = templateRenderer;
        _attachmentBuilder = attachmentBuilder;
    }

    public async Task<EmailMessage> BuildInvoiceEmailAsync(
        Invoice invoice,
        Company company,
        string recipientEmail,
        string? customMessage,
        EmailAttachmentFormat format,
        string? ccEmails,
        string? bccEmails)
    {
        var senderName = !string.IsNullOrWhiteSpace(company.EmailSenderName)
            ? company.EmailSenderName
            : company.BusinessName ?? company.OwnerFullName;

        var attachments = await _attachmentBuilder.BuildInvoiceAttachmentsAsync(
            invoice.Id,
            invoice.InvoiceNumber,
            format);

        return new EmailMessage
        {
            RecipientEmail = recipientEmail,
            Subject = $"Ihre Rechnung {invoice.InvoiceNumber} - {senderName}",
            ContentHtml = _templateRenderer.RenderContentHtml(invoice, company),
            ContentText = _templateRenderer.RenderContentText(invoice, company),
            CcEmails = ccEmails,
            BccEmails = bccEmails,
            Greeting = _templateRenderer.ResolveGreeting(invoice, customMessage),
            Attachments = attachments
        };
    }
}
