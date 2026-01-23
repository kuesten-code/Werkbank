using Kuestencode.Core.Models;
using Kuestencode.Faktura.Models;
using MimeKit;

namespace Kuestencode.Faktura.Services.Email;

/// <summary>
/// Builds complete email messages with templates and attachments
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

    public async Task<MimeMessage> BuildInvoiceEmailAsync(
        Invoice invoice,
        Company company,
        string recipientEmail,
        string? customMessage,
        EmailAttachmentFormat format,
        string? ccEmails,
        string? bccEmails)
    {
        var message = new MimeMessage();

        // Set sender
        message.From.Add(new MailboxAddress(
            !string.IsNullOrWhiteSpace(company.EmailSenderName)
                ? company.EmailSenderName
                : company.BusinessName ?? company.OwnerFullName,
            company.EmailSenderEmail ?? company.Email
        ));

        // Set recipient
        message.To.Add(MailboxAddress.Parse(recipientEmail));

        // Set subject
        message.Subject = $"Ihre Rechnung {invoice.InvoiceNumber} - {message.From}";

        // Add CC recipients
        AddRecipients(message.Cc, ccEmails);

        // Add BCC recipients
        AddRecipients(message.Bcc, bccEmails);

        // Build email body
        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = _templateRenderer.RenderHtmlBody(invoice, company, customMessage),
            TextBody = _templateRenderer.RenderPlainTextBody(invoice, company, customMessage)
        };

        // Add attachments
        await _attachmentBuilder.AddInvoiceAttachmentsAsync(
            bodyBuilder,
            invoice.Id,
            invoice.InvoiceNumber,
            format);

        message.Body = bodyBuilder.ToMessageBody();

        return message;
    }

    private void AddRecipients(InternetAddressList addressList, string? emailsString)
    {
        if (string.IsNullOrWhiteSpace(emailsString))
            return;

        var addresses = emailsString
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(e => e.Trim())
            .Where(e => !string.IsNullOrWhiteSpace(e));

        foreach (var address in addresses)
        {
            addressList.Add(MailboxAddress.Parse(address));
        }
    }
}
