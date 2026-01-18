using Kuestencode.Faktura.Models;

namespace Kuestencode.Faktura.Services.Email;

/// <summary>
/// Interface for rendering email templates
/// </summary>
public interface IEmailTemplateRenderer
{
    /// <summary>
    /// Renders an HTML email body for an invoice
    /// </summary>
    string RenderHtmlBody(Invoice invoice, Company company, string? customMessage);

    /// <summary>
    /// Renders a plain text email body for an invoice
    /// </summary>
    string RenderPlainTextBody(Invoice invoice, Company company, string? customMessage);
}
