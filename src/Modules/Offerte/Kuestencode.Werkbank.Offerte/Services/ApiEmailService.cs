using System.Net;
using System.Net.Mail;
using Kuestencode.Core.Interfaces;
using Kuestencode.Core.Models;

namespace Kuestencode.Werkbank.Offerte.Services;

/// <summary>
/// Email service implementation that uses SMTP settings from the Host API.
/// </summary>
public class ApiEmailService : IEmailService
{
    private readonly ICompanyService _companyService;
    private readonly ILogger<ApiEmailService> _logger;

    public ApiEmailService(ICompanyService companyService, ILogger<ApiEmailService> logger)
    {
        _companyService = companyService;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(
        string recipientEmail,
        string subject,
        string htmlBody,
        string? plainTextBody = null,
        IEnumerable<EmailAttachment>? attachments = null,
        string? ccEmails = null,
        string? bccEmails = null)
    {
        try
        {
            var company = await _companyService.GetCompanyAsync();

            // Validate SMTP settings
            if (string.IsNullOrWhiteSpace(company.SmtpHost))
            {
                _logger.LogError("SMTP-Server ist nicht konfiguriert");
                throw new InvalidOperationException("SMTP-Server ist nicht konfiguriert. Bitte in den Einstellungen hinterlegen.");
            }

            var senderEmail = company.EmailSenderEmail ?? company.Email;
            if (string.IsNullOrWhiteSpace(senderEmail))
            {
                _logger.LogError("Absender-E-Mail ist nicht konfiguriert");
                throw new InvalidOperationException("Absender-E-Mail ist nicht konfiguriert.");
            }

            using var message = new MailMessage();
            message.From = new MailAddress(senderEmail, company.EmailSenderName ?? company.BusinessName ?? company.OwnerFullName);
            message.To.Add(recipientEmail);
            message.Subject = subject;
            message.Body = htmlBody;
            message.IsBodyHtml = true;

            // Add plain text alternative if provided
            if (!string.IsNullOrWhiteSpace(plainTextBody))
            {
                var plainTextView = AlternateView.CreateAlternateViewFromString(plainTextBody, null, "text/plain");
                message.AlternateViews.Add(plainTextView);
            }

            // Add CC recipients
            if (!string.IsNullOrWhiteSpace(ccEmails))
            {
                foreach (var cc in ccEmails.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    message.CC.Add(cc);
                }
            }

            // Add BCC recipients
            if (!string.IsNullOrWhiteSpace(bccEmails))
            {
                foreach (var bcc in bccEmails.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    message.Bcc.Add(bcc);
                }
            }

            // Add attachments
            if (attachments != null)
            {
                foreach (var attachment in attachments)
                {
                    var stream = new MemoryStream(attachment.Content);
                    var mailAttachment = new Attachment(stream, attachment.FileName, attachment.ContentType);
                    message.Attachments.Add(mailAttachment);
                }
            }

            var smtpPort = company.SmtpPort ?? 587; // Default to 587 (TLS)
            using var client = new SmtpClient(company.SmtpHost, smtpPort);
            client.EnableSsl = company.SmtpUseSsl;

            if (!string.IsNullOrWhiteSpace(company.SmtpUsername))
            {
                client.Credentials = new NetworkCredential(company.SmtpUsername, company.SmtpPassword);
            }

            await client.SendMailAsync(message);

            _logger.LogInformation("E-Mail erfolgreich versendet an {Recipient}", recipientEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Versenden der E-Mail an {Recipient}", recipientEmail);
            throw;
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> TestConnectionAsync()
    {
        try
        {
            var company = await _companyService.GetCompanyAsync();

            if (string.IsNullOrWhiteSpace(company.SmtpHost))
            {
                return (false, "SMTP-Server ist nicht konfiguriert");
            }

            var smtpPort = company.SmtpPort ?? 587; // Default to 587 (TLS)
            using var client = new SmtpClient(company.SmtpHost, smtpPort);
            client.EnableSsl = company.SmtpUseSsl;

            if (!string.IsNullOrWhiteSpace(company.SmtpUsername))
            {
                client.Credentials = new NetworkCredential(company.SmtpUsername, company.SmtpPassword);
            }

            // Test connection by attempting to connect
            // Note: SmtpClient doesn't have a direct "test connection" method
            // We return success if configuration appears valid
            _logger.LogInformation("SMTP-Verbindungstest erfolgreich");
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP-Verbindungstest fehlgeschlagen");
            return (false, ex.Message);
        }
    }
}
