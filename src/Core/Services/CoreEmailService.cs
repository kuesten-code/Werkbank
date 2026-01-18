using Kuestencode.Core.Interfaces;
using Kuestencode.Core.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Kuestencode.Core.Services;

/// <summary>
/// Core email service implementation using MailKit.
/// Provides basic email functionality that can be used across modules.
/// </summary>
public class CoreEmailService : IEmailService
{
    private readonly ICompanyService _companyService;
    private readonly ILogger<CoreEmailService> _logger;

    public CoreEmailService(
        ICompanyService companyService,
        ILogger<CoreEmailService> logger)
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
            var smtpConfig = SmtpConfiguration.FromCompany(company);

            if (smtpConfig == null)
            {
                throw new InvalidOperationException("E-Mail-Konfiguration ist nicht vollständig. Bitte SMTP-Einstellungen prüfen.");
            }

            var message = BuildMessage(
                smtpConfig,
                recipientEmail,
                subject,
                htmlBody,
                plainTextBody,
                attachments,
                ccEmails,
                bccEmails);

            await SendMessageAsync(message, smtpConfig);

            _logger.LogInformation(
                "E-Mail erfolgreich gesendet an {Recipient} (Betreff: {Subject})",
                recipientEmail,
                subject);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Senden der E-Mail an {Recipient}", recipientEmail);
            throw;
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> TestConnectionAsync()
    {
        try
        {
            var company = await _companyService.GetCompanyAsync();
            var smtpConfig = SmtpConfiguration.FromCompany(company);

            if (smtpConfig == null)
            {
                return (false, "E-Mail-Konfiguration ist nicht vollständig.");
            }

            using var client = new SmtpClient();

            var secureSocketOptions = smtpConfig.UseSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;

            await client.ConnectAsync(smtpConfig.Host, smtpConfig.Port, secureSocketOptions);
            await client.AuthenticateAsync(smtpConfig.Username, smtpConfig.Password);
            await client.DisconnectAsync(true);

            _logger.LogInformation("SMTP-Verbindungstest erfolgreich");
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP-Verbindungstest fehlgeschlagen");
            return (false, $"Verbindungsfehler: {ex.Message}");
        }
    }

    private MimeMessage BuildMessage(
        SmtpConfiguration smtpConfig,
        string recipientEmail,
        string subject,
        string htmlBody,
        string? plainTextBody,
        IEnumerable<EmailAttachment>? attachments,
        string? ccEmails,
        string? bccEmails)
    {
        var message = new MimeMessage();

        // Set sender
        message.From.Add(new MailboxAddress(
            smtpConfig.SenderName ?? smtpConfig.SenderEmail,
            smtpConfig.SenderEmail));

        // Set recipient
        message.To.Add(MailboxAddress.Parse(recipientEmail));

        // Add CC recipients
        if (!string.IsNullOrWhiteSpace(ccEmails))
        {
            foreach (var cc in ParseEmailList(ccEmails))
            {
                message.Cc.Add(MailboxAddress.Parse(cc));
            }
        }

        // Add BCC recipients
        if (!string.IsNullOrWhiteSpace(bccEmails))
        {
            foreach (var bcc in ParseEmailList(bccEmails))
            {
                message.Bcc.Add(MailboxAddress.Parse(bcc));
            }
        }

        message.Subject = subject;

        // Build body
        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = htmlBody
        };

        if (!string.IsNullOrWhiteSpace(plainTextBody))
        {
            bodyBuilder.TextBody = plainTextBody;
        }

        // Add attachments
        if (attachments != null)
        {
            foreach (var attachment in attachments)
            {
                bodyBuilder.Attachments.Add(
                    attachment.FileName,
                    attachment.Content,
                    ContentType.Parse(attachment.ContentType));
            }
        }

        message.Body = bodyBuilder.ToMessageBody();

        return message;
    }

    private async Task SendMessageAsync(MimeMessage message, SmtpConfiguration smtpConfig)
    {
        using var client = new SmtpClient();

        var secureSocketOptions = smtpConfig.UseSsl
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.None;

        await client.ConnectAsync(smtpConfig.Host, smtpConfig.Port, secureSocketOptions);
        await client.AuthenticateAsync(smtpConfig.Username, smtpConfig.Password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    private static IEnumerable<string> ParseEmailList(string emailList)
    {
        return emailList
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(e => e.Trim())
            .Where(e => !string.IsNullOrWhiteSpace(e));
    }
}
