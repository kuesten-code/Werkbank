using Kuestencode.Core.Interfaces;
using Kuestencode.Shared.Contracts.Host;

namespace Kuestencode.Shared.ApiClients;

/// <summary>
/// Implementiert <see cref="IEmailEngine"/> per HTTP gegen den zentralen Email-Endpoint im Host.
/// Fachmodule (Faktura, Rapport, Offerte, ...) registrieren diese Implementierung statt selbst
/// per SMTP zu versenden — nur Host besitzt noch echte SMTP-Zugangsdaten.
/// </summary>
public class RemoteEmailEngine : IEmailEngine
{
    private readonly IHostApiClient _hostApiClient;

    public RemoteEmailEngine(IHostApiClient hostApiClient)
    {
        _hostApiClient = hostApiClient;
    }

    public Task<bool> SendEmailAsync(
        string recipientEmail,
        string subject,
        string contentHtml,
        string? contentText = null,
        IEnumerable<EmailAttachment>? attachments = null,
        string? ccEmails = null,
        string? bccEmails = null,
        string? greeting = null,
        bool includeClosing = true)
    {
        var request = new SendEmailRequest
        {
            RecipientEmail = recipientEmail,
            Subject = subject,
            ContentHtml = contentHtml,
            ContentText = contentText,
            CcEmails = ccEmails,
            BccEmails = bccEmails,
            Greeting = greeting,
            IncludeClosing = includeClosing,
            Attachments = attachments?.Select(a => new EmailAttachmentDto
            {
                FileName = a.FileName,
                Content = a.Content,
                ContentType = a.ContentType
            }).ToList() ?? []
        };

        return _hostApiClient.SendEmailAsync(request);
    }

    public Task<bool> SendTemplatedEmailAsync<TContext>(
        string templateName,
        TContext context,
        string recipientEmail,
        string subject,
        IEnumerable<EmailAttachment>? attachments = null,
        string? ccEmails = null,
        string? bccEmails = null)
    {
        throw new NotSupportedException(
            "SendTemplatedEmailAsync wird über RemoteEmailEngine nicht unterstützt — " +
            "es gibt keine modulübergreifenden IEmailTemplateProvider. Inhalt vorab selbst rendern " +
            "und SendEmailAsync verwenden.");
    }

    public Task<(bool Success, string? ErrorMessage)> TestConnectionAsync()
    {
        return _hostApiClient.TestEmailConnectionAsync();
    }
}
