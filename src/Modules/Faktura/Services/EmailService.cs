using Kuestencode.Core.Enums;
using Kuestencode.Core.Models;
using Kuestencode.Faktura.Data.Repositories;
using Kuestencode.Faktura.Models;
using Kuestencode.Faktura.Services.Email;
using Kuestencode.Shared.ApiClients;

namespace Kuestencode.Faktura.Services;

/// <summary>
/// Orchestrates email sending for invoices
/// </summary>
public class EmailService : IEmailService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IHostApiClient _hostApiClient;
    private readonly IEmailMessageBuilder _messageBuilder;
    private readonly ISmtpClient _smtpClient;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IInvoiceRepository invoiceRepository,
        IHostApiClient hostApiClient,
        IEmailMessageBuilder messageBuilder,
        ISmtpClient smtpClient,
        ILogger<EmailService> logger)
    {
        _invoiceRepository = invoiceRepository;
        _hostApiClient = hostApiClient;
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
            _logger.LogInformation(
                "EmailService: start send (InvoiceId={InvoiceId}, Recipient={Recipient})",
                invoiceId,
                recipientEmail);

            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                throw new ArgumentException("Empfänger-E-Mail-Adresse ist erforderlich", nameof(recipientEmail));
            }

            // Load company with email settings via Host API
            _logger.LogInformation("EmailService: loading company settings (InvoiceId={InvoiceId})", invoiceId);
            var companyDto = await _hostApiClient.GetCompanyAsync();
            if (companyDto == null)
            {
                throw new InvalidOperationException("Firmendaten nicht gefunden");
            }

            var company = new Company
            {
                Id = companyDto.Id,
                OwnerFullName = companyDto.OwnerFullName,
                BusinessName = companyDto.BusinessName,
                Email = companyDto.Email,
                SmtpHost = companyDto.SmtpHost,
                SmtpPort = companyDto.SmtpPort,
                SmtpUseSsl = companyDto.SmtpUseSsl,
                SmtpUsername = companyDto.SmtpUsername,
                SmtpPassword = companyDto.SmtpPassword,
                EmailSenderEmail = companyDto.EmailSenderEmail,
                EmailSenderName = companyDto.EmailSenderName,
                EmailSignature = companyDto.EmailSignature,
                EmailLayout = Enum.Parse<EmailLayout>(companyDto.EmailLayout),
                EmailPrimaryColor = companyDto.EmailPrimaryColor,
                EmailAccentColor = companyDto.EmailAccentColor,
                EmailGreeting = companyDto.EmailGreeting,
                EmailClosing = companyDto.EmailClosing
            };

            // Load invoice with details
            _logger.LogInformation("EmailService: loading invoice (InvoiceId={InvoiceId})", invoiceId);
            var invoice = await _invoiceRepository.GetWithDetailsAsync(invoiceId);
            if (invoice == null)
            {
                throw new InvalidOperationException($"Rechnung mit ID {invoiceId} nicht gefunden");
            }

            // Build email message using dedicated builder
            _logger.LogInformation("EmailService: building message (InvoiceId={InvoiceId})", invoiceId);
            var message = await _messageBuilder.BuildInvoiceEmailAsync(
                invoice,
                company,
                recipientEmail,
                customMessage,
                format,
                ccEmails,
                bccEmails);

            // Send email using SMTP client
            _logger.LogInformation("EmailService: sending via SMTP (InvoiceId={InvoiceId})", invoiceId);
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
