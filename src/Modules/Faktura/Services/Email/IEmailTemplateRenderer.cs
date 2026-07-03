using Kuestencode.Core.Models;
using Kuestencode.Faktura.Models;

namespace Kuestencode.Faktura.Services.Email;

/// <summary>
/// Interface for rendering the invoice-specific content of an email.
/// Layout/Farben/Anrede/Grußformel/Signatur werden zentral vom Host-EmailEngine ergänzt —
/// dieser Renderer liefert nur noch den fachlichen Inhalt (Rechnungsdetails, Bankverbindung).
/// </summary>
public interface IEmailTemplateRenderer
{
    /// <summary>
    /// Renders the HTML content fragment for an invoice (details table, bank info, PDF hint).
    /// </summary>
    string RenderContentHtml(Invoice invoice, Company company);

    /// <summary>
    /// Renders the plain text content fragment for an invoice.
    /// </summary>
    string RenderContentText(Invoice invoice, Company company);

    /// <summary>
    /// Bestimmt die Anrede-Überschreibung: benutzerdefinierte Nachricht > kundenspezifische
    /// Anrede > null (Host nutzt dann den Standardgruß aus den Firmeneinstellungen).
    /// </summary>
    string? ResolveGreeting(Invoice invoice, string? customMessage);
}
