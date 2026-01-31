using Kuestencode.Core.Enums;
using Kuestencode.Core.Models;
using Kuestencode.Werkbank.Offerte.Domain.Entities;
using System.Globalization;

namespace Kuestencode.Werkbank.Offerte.Services.Email;

/// <summary>
/// Renders HTML and plain text email templates for Angebote.
/// </summary>
public class OfferteEmailTemplateRenderer : IOfferteEmailTemplateRenderer
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<OfferteEmailTemplateRenderer> _logger;

    public OfferteEmailTemplateRenderer(
        IWebHostEnvironment environment,
        ILogger<OfferteEmailTemplateRenderer> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public string RenderHtmlBody(
        Angebot angebot,
        Customer kunde,
        Company firma,
        OfferteSettings settings,
        string? customMessage = null,
        bool includeClosing = true)
    {
        var culture = new CultureInfo("de-DE");
        var formattedTotal = angebot.Bruttosumme.ToString("C", culture);
        var formattedDate = angebot.Erstelldatum.ToString("dd.MM.yyyy", culture);
        var formattedGueltigBis = angebot.GueltigBis.ToString("dd.MM.yyyy", culture);

        var templateName = settings.EmailLayout switch
        {
            EmailLayout.Klar => "OfferteEmailTemplateKlar.html",
            EmailLayout.Strukturiert => "OfferteEmailTemplateStrukturiert.html",
            EmailLayout.Betont => "OfferteEmailTemplateBetont.html",
            _ => "OfferteEmailTemplateKlar.html"
        };

        var templatePath = Path.Combine(_environment.WebRootPath, "templates", templateName);

        if (!File.Exists(templatePath))
        {
            _logger.LogWarning("Email template {TemplateName} not found at {TemplatePath}, using fallback", templateName, templatePath);
            return BuildFallbackHtmlBody(angebot, kunde, firma, settings, customMessage);
        }

        _logger.LogDebug("Rendering email with layout {Layout}, primary color {PrimaryColor}, accent color {AccentColor}",
            settings.EmailLayout, settings.EmailPrimaryColor, settings.EmailAccentColor);

        var template = File.ReadAllText(templatePath);

        // Replace colors (handle null and empty strings)
        var primaryColor = string.IsNullOrWhiteSpace(settings.EmailPrimaryColor) ? "#0F2A3D" : settings.EmailPrimaryColor;
        var accentColor = string.IsNullOrWhiteSpace(settings.EmailAccentColor) ? "#3FA796" : settings.EmailAccentColor;
        template = template.Replace("{{PRIMARY_COLOR}}", primaryColor);
        template = template.Replace("{{ACCENT_COLOR}}", accentColor);

        // Prepare greeting
        string greeting;
        if (!string.IsNullOrWhiteSpace(customMessage))
        {
            greeting = customMessage;
        }
        else if (!string.IsNullOrWhiteSpace(kunde.Salutation))
        {
            greeting = $"{kunde.Salutation}\n\nanbei erhalten Sie unser Angebot.";
        }
        else
        {
            greeting = string.IsNullOrWhiteSpace(settings.EmailGreeting)
                ? "Sehr geehrte Damen und Herren,\n\nanbei erhalten Sie unser Angebot."
                : settings.EmailGreeting;
        }

        // Prepare closing
        var firmenname = !string.IsNullOrEmpty(firma.BusinessName)
            ? firma.BusinessName
            : firma.OwnerFullName;

        var closing = includeClosing
            ? (string.IsNullOrWhiteSpace(settings.EmailClosing)
                ? $"Mit freundlichen Grüßen\n\n{firmenname}"
                : settings.EmailClosing)
            : string.Empty;

        greeting = ReplacePlaceholders(greeting, angebot.Angebotsnummer, formattedGueltigBis, firmenname);
        closing = ReplacePlaceholders(closing, angebot.Angebotsnummer, formattedGueltigBis, firmenname);

        // Replace template placeholders
        template = template.Replace("{{GREETING}}", greeting.Replace("\n", "<br>"));
        template = template.Replace("{{CLOSING}}", closing.Replace("\n", "<br>"));
        template = template.Replace("{{ANGEBOT_NUMBER}}", angebot.Angebotsnummer);
        template = template.Replace("{{ANGEBOT_DATE}}", formattedDate);
        template = template.Replace("{{GUELTIG_BIS}}", formattedGueltigBis);
        template = template.Replace("{{TOTAL_AMOUNT}}", formattedTotal);

        return template;
    }

    public string RenderPlainTextBody(
        Angebot angebot,
        Customer kunde,
        Company firma,
        OfferteSettings settings,
        string? customMessage = null,
        bool includeClosing = true)
    {
        var culture = new CultureInfo("de-DE");
        var formattedTotal = angebot.Bruttosumme.ToString("C", culture);
        var formattedDate = angebot.Erstelldatum.ToString("dd.MM.yyyy", culture);
        var formattedGueltigBis = angebot.GueltigBis.ToString("dd.MM.yyyy", culture);

        // Prepare greeting
        string greetingText;
        if (!string.IsNullOrWhiteSpace(customMessage))
        {
            greetingText = customMessage;
        }
        else if (!string.IsNullOrWhiteSpace(kunde.Salutation))
        {
            greetingText = $"{kunde.Salutation}\n\nanbei erhalten Sie unser Angebot {angebot.Angebotsnummer}.";
        }
        else
        {
            greetingText = $"Sehr geehrte Damen und Herren,\n\nanbei erhalten Sie unser Angebot {angebot.Angebotsnummer}.";
        }

        var firmenname = firma.BusinessName ?? firma.OwnerFullName;

        // Prepare closing section
        var closingText = includeClosing
            ? $"\n\n{(!string.IsNullOrWhiteSpace(settings.EmailClosing) ? settings.EmailClosing : "Mit freundlichen Grüßen")}\n\n--\n{firmenname}\n{firma.Address}\n{firma.PostalCode} {firma.City}\n{(!string.IsNullOrWhiteSpace(firma.Phone) ? $"Tel: {firma.Phone}\n" : "")}{(!string.IsNullOrWhiteSpace(firma.Email) ? $"E-Mail: {firma.Email}\n" : "")}{(!string.IsNullOrWhiteSpace(firma.Website) ? $"Web: {firma.Website}\n" : "")}"
            : string.Empty;

        var text = $@"
{firmenname}
{new string('=', firmenname.Length)}

{greetingText}

ANGEBOTSDETAILS:
------------------
Angebotsbetrag:   {formattedTotal}
Angebotsnummer:   {angebot.Angebotsnummer}
Angebotsdatum:    {formattedDate}
Gültig bis:       {formattedGueltigBis}

Das Angebot finden Sie im Anhang dieser E-Mail als PDF-Datei.

Bei Fragen stehen wir Ihnen gerne zur Verfügung.{closingText}";

        return text.Trim();
    }

    private string ReplacePlaceholders(string text, string angebotsnummer, string gueltigBis, string firmenname)
    {
        return text
            .Replace("{{Firmenname}}", firmenname)
            .Replace("{{Angebotsnummer}}", angebotsnummer)
            .Replace("{{Gueltigkeitsdatum}}", gueltigBis);
    }

    private string BuildFallbackHtmlBody(
        Angebot angebot,
        Customer kunde,
        Company firma,
        OfferteSettings settings,
        string? customMessage)
    {
        var culture = new CultureInfo("de-DE");
        var formattedTotal = angebot.Bruttosumme.ToString("C", culture);
        var formattedDate = angebot.Erstelldatum.ToString("dd.MM.yyyy", culture);
        var formattedGueltigBis = angebot.GueltigBis.ToString("dd.MM.yyyy", culture);

        var primaryColor = string.IsNullOrWhiteSpace(settings.EmailPrimaryColor) ? "#0F2A3D" : settings.EmailPrimaryColor;
        var accentColor = string.IsNullOrWhiteSpace(settings.EmailAccentColor) ? "#3FA796" : settings.EmailAccentColor;
        var firmenname = firma.BusinessName ?? firma.OwnerFullName;

        var html = $@"
<!DOCTYPE html>
<html lang=""de"">
<head>
    <meta charset=""UTF-8"">
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: {primaryColor}; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f8f9fa; padding: 20px; margin: 20px 0; }}
        .details {{ background-color: white; padding: 15px; margin: 15px 0; border-left: 3px solid {accentColor}; }}
        .footer {{ text-align: center; color: #666; font-size: 12px; margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; }}
        .highlight {{ color: {primaryColor}; font-weight: bold; }}
        table {{ width: 100%; border-collapse: collapse; }}
        td {{ padding: 8px 0; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>{firmenname}</h1>
        </div>

        <div class=""content"">
            <p>{(!string.IsNullOrWhiteSpace(kunde.Salutation) ? kunde.Salutation : "Sehr geehrte Damen und Herren,")}</p>

            {(!string.IsNullOrWhiteSpace(customMessage) ? $"<p>{customMessage.Replace("\n", "<br>")}</p>" : "")}

            <p>anbei erhalten Sie unser Angebot <span class=""highlight"">{angebot.Angebotsnummer}</span>.</p>

            <div class=""details"">
                <table>
                    <tr>
                        <td><strong>Angebotsbetrag:</strong></td>
                        <td class=""highlight"">{formattedTotal}</td>
                    </tr>
                    <tr>
                        <td><strong>Angebotsnummer:</strong></td>
                        <td>{angebot.Angebotsnummer}</td>
                    </tr>
                    <tr>
                        <td><strong>Angebotsdatum:</strong></td>
                        <td>{formattedDate}</td>
                    </tr>
                    <tr>
                        <td><strong>Gültig bis:</strong></td>
                        <td>{formattedGueltigBis}</td>
                    </tr>
                </table>
            </div>

            <p>Das Angebot finden Sie im Anhang dieser E-Mail als PDF-Datei.</p>
            <p>Bei Fragen stehen wir Ihnen gerne zur Verfügung.</p>

            <p>Mit freundlichen Grüßen<br>{firmenname}</p>
        </div>

        <div class=""footer"">
            <p><strong>{firmenname}</strong></p>
            <p>{firma.Address}, {firma.PostalCode} {firma.City}</p>
            {(!string.IsNullOrWhiteSpace(firma.Phone) ? $"<p>Tel: {firma.Phone}</p>" : "")}
            {(!string.IsNullOrWhiteSpace(firma.Email) ? $"<p>E-Mail: {firma.Email}</p>" : "")}
            {(!string.IsNullOrWhiteSpace(firma.Website) ? $"<p>Web: {firma.Website}</p>" : "")}
        </div>
    </div>
</body>
</html>";

        return html;
    }
}
