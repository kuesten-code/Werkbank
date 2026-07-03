using Kuestencode.Core.Models;
using Kuestencode.Werkbank.Offerte.Domain.Entities;

namespace Kuestencode.Werkbank.Offerte.Services.Email;

/// <summary>
/// Interface for rendering the Angebot-specific content of an email.
/// Layout/Farben/Anrede/Grußformel/Signatur werden zentral vom Host-EmailEngine ergänzt —
/// dieser Renderer liefert nur noch den fachlichen Inhalt (Angebotsdetails).
/// </summary>
public interface IOfferteEmailTemplateRenderer
{
    /// <summary>
    /// Renders the HTML content fragment for an Angebot.
    /// </summary>
    string RenderContentHtml(Angebot angebot);

    /// <summary>
    /// Renders the plain text content fragment for an Angebot.
    /// </summary>
    string RenderContentText(Angebot angebot);

    /// <summary>
    /// Bestimmt die Anrede-Überschreibung: benutzerdefinierte Nachricht > kundenspezifische
    /// Anrede > null (Host nutzt dann den Standardgruß aus den Firmeneinstellungen).
    /// </summary>
    string? ResolveGreeting(Customer kunde, string? customMessage);
}
