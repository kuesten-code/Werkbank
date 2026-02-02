using Kuestencode.Core.Models;
using Kuestencode.Werkbank.Offerte.Domain.Entities;

namespace Kuestencode.Werkbank.Offerte.Services.Email;

/// <summary>
/// Interface for rendering Offerte email templates.
/// </summary>
public interface IOfferteEmailTemplateRenderer
{
    /// <summary>
    /// Renders the HTML email body for an Angebot.
    /// </summary>
    /// <param name="angebot">The Angebot to render.</param>
    /// <param name="kunde">The customer.</param>
    /// <param name="firma">The company.</param>
    /// <param name="settings">The Offerte settings.</param>
    /// <param name="customMessage">Optional custom message.</param>
    /// <param name="includeClosing">Whether to include the closing text.</param>
    /// <returns>The rendered HTML body.</returns>
    string RenderHtmlBody(
        Angebot angebot,
        Customer kunde,
        Company firma,
        OfferteSettings settings,
        string? customMessage = null,
        bool includeClosing = true);

    /// <summary>
    /// Renders the plain text email body for an Angebot.
    /// </summary>
    /// <param name="angebot">The Angebot to render.</param>
    /// <param name="kunde">The customer.</param>
    /// <param name="firma">The company.</param>
    /// <param name="settings">The Offerte settings.</param>
    /// <param name="customMessage">Optional custom message.</param>
    /// <param name="includeClosing">Whether to include the closing text.</param>
    /// <returns>The rendered plain text body.</returns>
    string RenderPlainTextBody(
        Angebot angebot,
        Customer kunde,
        Company firma,
        OfferteSettings settings,
        string? customMessage = null,
        bool includeClosing = true);
}
