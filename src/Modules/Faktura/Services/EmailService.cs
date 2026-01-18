using Kuestencode.Faktura.Data.Repositories;
using Kuestencode.Faktura.Models;
using Kuestencode.Faktura.Services.Email;

namespace Kuestencode.Faktura.Services;

/// <summary>
/// Orchestrates email sending for invoices
/// </summary>
public class EmailService : IEmailService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ICompanyService _companyService;
    private readonly IEmailMessageBuilder _messageBuilder;
    private readonly ISmtpClient _smtpClient;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IInvoiceRepository invoiceRepository,
        ICompanyService companyService,
        IEmailMessageBuilder messageBuilder,
        ISmtpClient smtpClient,
        ILogger<EmailService> logger)
    {
        _invoiceRepository = invoiceRepository;
        _companyService = companyService;
        _messageBuilder = messageBuilder;
        _smtpClient = smtpClient;
        _logger = logger;
    }

    public async Task<bool> SendInvoiceEmailAsync(
        int invoiceId,
        string recipientEmail,
        string? customMessage = null,
        EmailAttachmentFormat format = EmailAttachmentFormat.NormalPdf,
        string? ccEmails = null,
        string? bccEmails = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                throw new ArgumentException("Empfänger-E-Mail-Adresse ist erforderlich", nameof(recipientEmail));
            }

            // Load company with email settings
            var company = await _companyService.GetCompanyAsync();

            // Load invoice with details
            var invoice = await _invoiceRepository.GetWithDetailsAsync(invoiceId);
            if (invoice == null)
            {
                throw new InvalidOperationException($"Rechnung mit ID {invoiceId} nicht gefunden");
            }

            // Build email message using dedicated builder
            var message = await _messageBuilder.BuildInvoiceEmailAsync(
                invoice,
                company,
                recipientEmail,
                customMessage,
                format,
                ccEmails,
                bccEmails);

            // Send email using SMTP client
            await _smtpClient.SendAsync(message, company);

            // Update invoice with email tracking
            await UpdateInvoiceAfterSendAsync(invoice, recipientEmail, ccEmails, bccEmails);

            _logger.LogInformation(
                "Rechnung {InvoiceNumber} erfolgreich an {Email} versendet (Format: {Format})",
                invoice.InvoiceNumber,
                recipientEmail,
                format);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Versenden der Rechnung {InvoiceId} an {Email}", invoiceId, recipientEmail);
            throw;
        }
    }

    private async Task UpdateInvoiceAfterSendAsync(
        Invoice invoice,
        string recipientEmail,
        string? ccEmails,
        string? bccEmails)
    {
        invoice.EmailSentAt = DateTime.UtcNow;
        invoice.EmailSentTo = recipientEmail;
        invoice.EmailSendCount++;
        invoice.EmailCcRecipients = ccEmails;
        invoice.EmailBccRecipients = bccEmails;

        if (invoice.Status == InvoiceStatus.Draft)
        {
            invoice.Status = InvoiceStatus.Sent;
        }

        await _invoiceRepository.UpdateAsync(invoice);
    }

    public async Task<(bool success, string? errorMessage)> TestEmailConnectionAsync(Company company)
    {
        return await _smtpClient.TestConnectionAsync(company);
    }
}
