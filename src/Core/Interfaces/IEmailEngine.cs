using Kuestencode.Core.Models;

namespace Kuestencode.Core.Interfaces;

/// <summary>
/// Generische Email-Engine für plattformweite Email-Funktionalität.
/// Module können eigene IEmailTemplateProvider registrieren.
/// </summary>
public interface IEmailEngine
{
    /// <summary>
    /// Sendet eine Email mit optionalen Anhängen.
    /// </summary>
    Task<bool> SendEmailAsync(
        string recipientEmail,
        string subject,
        string htmlBody,
        string? plainTextBody = null,
        IEnumerable<EmailAttachment>? attachments = null,
        string? ccEmails = null,
        string? bccEmails = null);

    /// <summary>
    /// Sendet eine template-basierte Email.
    /// </summary>
    /// <typeparam name="TContext">Der Typ des Template-Kontexts</typeparam>
    /// <param name="templateName">Name des Templates (z.B. "invoice", "project-status")</param>
    /// <param name="context">Der Kontext für das Template</param>
    /// <param name="recipientEmail">Empfänger-Email</param>
    /// <param name="subject">Betreff</param>
    /// <param name="attachments">Optionale Anhänge</param>
    /// <param name="ccEmails">CC-Empfänger (komma-getrennt)</param>
    /// <param name="bccEmails">BCC-Empfänger (komma-getrennt)</param>
    Task<bool> SendTemplatedEmailAsync<TContext>(
        string templateName,
        TContext context,
        string recipientEmail,
        string subject,
        IEnumerable<EmailAttachment>? attachments = null,
        string? ccEmails = null,
        string? bccEmails = null);

    /// <summary>
    /// Testet die SMTP-Verbindung.
    /// </summary>
    Task<(bool Success, string? ErrorMessage)> TestConnectionAsync();
}

/// <summary>
/// Interface für Email-Template-Provider.
/// Module registrieren ihre eigenen Template-Provider.
/// </summary>
public interface IEmailTemplateProvider
{
    /// <summary>
    /// Der eindeutige Name des Templates (z.B. "invoice", "project-status").
    /// </summary>
    string TemplateName { get; }

    /// <summary>
    /// Rendert den HTML-Body des Templates.
    /// </summary>
    string RenderHtml<TContext>(TContext context, Company company);

    /// <summary>
    /// Rendert den Plain-Text-Body des Templates.
    /// </summary>
    string RenderPlainText<TContext>(TContext context, Company company);
}
