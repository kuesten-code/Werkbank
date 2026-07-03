using Kuestencode.Core.Enums;
using Kuestencode.Core.Models;

namespace Kuestencode.Core.Services;

/// <summary>
/// Wickelt Email-Inhalt in eines von drei firmenweiten Layouts (Farben/Header/Footer/Anrede/
/// Grußformel/Signatur aus <see cref="Company"/>, Auswahl über <see cref="Company.EmailLayout"/>).
/// Einzige Quelle für dieses Layout — wird sowohl vom tatsächlichen Versand (Host-EmailEngine)
/// als auch von den Live-Vorschau-Komponenten genutzt, damit beide garantiert gleich aussehen.
/// </summary>
public static class EmailTemplateRenderer
{
    public static string WrapHtml(Company company, string contentHtml, string? greeting, bool includeClosing)
    {
        var greetingText = ReplaceFirmenname(greeting ?? company.EmailGreeting ?? "Sehr geehrte Damen und Herren,", company).Replace("\n", "<br>");
        var closingText = includeClosing
            ? ReplaceFirmenname(company.EmailClosing ?? "Mit freundlichen Grüßen", company).Replace("\n", "<br>")
            : string.Empty;
        var signatureText = ReplaceFirmenname(company.EmailSignature ?? company.DisplayName, company).Replace("\n", "<br>");

        var template = company.EmailLayout switch
        {
            EmailLayout.Strukturiert => StrukturiertTemplate,
            EmailLayout.Betont => BetontTemplate,
            _ => KlarTemplate
        };

        return template
            .Replace("{{PRIMARY_COLOR}}", company.EmailPrimaryColor)
            .Replace("{{ACCENT_COLOR}}", company.EmailAccentColor)
            .Replace("{{COMPANY_NAME}}", company.DisplayName)
            .Replace("{{GREETING}}", greetingText)
            .Replace("{{CONTENT}}", contentHtml)
            .Replace("{{CLOSING}}", closingText)
            .Replace("{{SIGNATURE}}", signatureText)
            .Replace("{{COMPANY_ADDRESS}}", company.GetFormattedAddress().Replace("\n", "<br>"))
            .Replace("{{COMPANY_CONTACT}}", BuildContactLine(company))
            .Replace("{{COMPANY_TAX_INFO}}", BuildTaxInfoLine(company));
    }

    public static string WrapText(Company company, string contentText, string? greeting, bool includeClosing)
    {
        var greetingText = ReplaceFirmenname(greeting ?? company.EmailGreeting ?? "Sehr geehrte Damen und Herren,", company);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine(greetingText);
        sb.AppendLine();
        sb.AppendLine(contentText);

        if (includeClosing)
        {
            sb.AppendLine();
            sb.AppendLine(ReplaceFirmenname(company.EmailClosing ?? "Mit freundlichen Grüßen", company));
        }

        sb.AppendLine();
        sb.AppendLine(ReplaceFirmenname(company.EmailSignature ?? company.DisplayName, company));
        sb.AppendLine();
        sb.AppendLine("--");
        sb.AppendLine(company.DisplayName);
        sb.AppendLine(company.GetFormattedAddress());
        if (!string.IsNullOrWhiteSpace(company.Phone)) sb.AppendLine($"Tel: {company.Phone}");
        if (!string.IsNullOrWhiteSpace(company.Email)) sb.AppendLine($"E-Mail: {company.Email}");
        if (!string.IsNullOrWhiteSpace(company.Website)) sb.AppendLine($"Web: {company.Website}");
        if (!string.IsNullOrWhiteSpace(company.TaxNumber)) sb.AppendLine($"Steuernummer: {company.TaxNumber}");
        if (!string.IsNullOrWhiteSpace(company.VatId)) sb.AppendLine($"USt-IdNr: {company.VatId}");

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Ersetzt den Firmenname-Platzhalter in Anrede/Grußformel/Signatur-Texten — dieser Token
    /// stammt aus der alten, modulspezifischen Template-Syntax und kann noch in bestehenden
    /// Firmeneinstellungen gespeichert sein.
    /// </summary>
    private static string ReplaceFirmenname(string text, Company company) =>
        text.Replace("{{Firmenname}}", company.DisplayName);

    private static string BuildContactLine(Company company)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(company.Phone)) parts.Add($"Tel: {company.Phone}");
        if (!string.IsNullOrWhiteSpace(company.Email)) parts.Add($"E-Mail: {company.Email}");
        if (!string.IsNullOrWhiteSpace(company.Website)) parts.Add($"Web: {company.Website}");
        return string.Join(" · ", parts);
    }

    private static string BuildTaxInfoLine(Company company)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(company.TaxNumber)) parts.Add($"Steuernummer: {company.TaxNumber}");
        if (!string.IsNullOrWhiteSpace(company.VatId)) parts.Add($"USt-IdNr: {company.VatId}");
        return string.Join(" · ", parts);
    }

    // Alle drei Templates nutzen inline Styles statt eines <style>-Blocks — viele Email-Clients
    // (v.a. Outlook) entfernen <style>-Blöcke, inline Styles funktionieren überall zuverlässig.

    private const string KlarTemplate = """
        <!DOCTYPE html>
        <html lang="de">
        <body style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif; line-height: 1.6; color: #1a1a1a; margin: 0; padding: 0;">
            <div style="max-width: 560px; margin: 0 auto; padding: 20px;">
                <h1 style="color: {{PRIMARY_COLOR}}; font-size: 22px; margin: 0 0 20px 0; font-weight: 600;">{{COMPANY_NAME}}</h1>

                <p style="white-space: pre-line; margin: 0 0 20px 0;">{{GREETING}}</p>

                {{CONTENT}}

                <p style="white-space: pre-line; margin: 20px 0 0 0;">{{CLOSING}}</p>
                <p style="white-space: pre-line; margin: 8px 0 0 0;">{{SIGNATURE}}</p>

                <div style="margin-top: 30px; padding-top: 15px; border-top: 1px solid #e5e7eb; color: #666; font-size: 12px;">
                    <p style="margin: 0 0 4px 0;"><strong>{{COMPANY_NAME}}</strong></p>
                    <p style="margin: 0 0 4px 0;">{{COMPANY_ADDRESS}}</p>
                    <p style="margin: 0 0 4px 0;">{{COMPANY_CONTACT}}</p>
                    <p style="margin: 0;">{{COMPANY_TAX_INFO}}</p>
                </div>
            </div>
        </body>
        </html>
        """;

    private const string StrukturiertTemplate = """
        <!DOCTYPE html>
        <html lang="de">
        <body style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif; line-height: 1.6; color: #1a1a1a; margin: 0; padding: 0;">
            <div style="max-width: 560px; margin: 0 auto; padding: 20px;">
                <h1 style="color: {{PRIMARY_COLOR}}; font-size: 22px; margin: 0 0 20px 0; font-weight: 600;">{{COMPANY_NAME}}</h1>

                <p style="white-space: pre-line; margin: 0 0 20px 0;">{{GREETING}}</p>

                <div style="border: 1px solid #e5e7eb; border-radius: 6px; padding: 18px; margin: 20px 0;">
                    {{CONTENT}}
                </div>

                <p style="white-space: pre-line; margin: 20px 0 0 0;">{{CLOSING}}</p>
                <p style="white-space: pre-line; margin: 8px 0 0 0;">{{SIGNATURE}}</p>

                <div style="margin-top: 30px; padding-top: 15px; border-top: 2px solid {{ACCENT_COLOR}}; color: #666; font-size: 12px;">
                    <p style="margin: 0 0 4px 0;"><strong>{{COMPANY_NAME}}</strong></p>
                    <p style="margin: 0 0 4px 0;">{{COMPANY_ADDRESS}}</p>
                    <p style="margin: 0 0 4px 0;">{{COMPANY_CONTACT}}</p>
                    <p style="margin: 0;">{{COMPANY_TAX_INFO}}</p>
                </div>
            </div>
        </body>
        </html>
        """;

    private const string BetontTemplate = """
        <!DOCTYPE html>
        <html lang="de">
        <body style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif; line-height: 1.6; color: #1a1a1a; margin: 0; padding: 0;">
            <div style="max-width: 560px; margin: 0 auto;">
                <div style="background-color: {{PRIMARY_COLOR}}; color: #ffffff; padding: 24px 20px;">
                    <h1 style="margin: 0; font-size: 22px; font-weight: 600; color: #ffffff;">{{COMPANY_NAME}}</h1>
                </div>

                <div style="padding: 24px 20px;">
                    <p style="white-space: pre-line; margin: 0 0 20px 0;">{{GREETING}}</p>

                    <div style="background-color: #f9fafb; border-left: 4px solid {{ACCENT_COLOR}}; padding: 16px 18px; margin: 20px 0;">
                        {{CONTENT}}
                    </div>

                    <p style="white-space: pre-line; margin: 20px 0 0 0;">{{CLOSING}}</p>
                    <p style="white-space: pre-line; margin: 8px 0 0 0;">{{SIGNATURE}}</p>

                    <div style="margin-top: 30px; padding-top: 15px; border-top: 1px solid {{ACCENT_COLOR}}; color: #666; font-size: 12px;">
                        <p style="margin: 0 0 4px 0;"><strong>{{COMPANY_NAME}}</strong></p>
                        <p style="margin: 0 0 4px 0;">{{COMPANY_ADDRESS}}</p>
                        <p style="margin: 0 0 4px 0;">{{COMPANY_CONTACT}}</p>
                        <p style="margin: 0;">{{COMPANY_TAX_INFO}}</p>
                    </div>
                </div>
            </div>
        </body>
        </html>
        """;
}
