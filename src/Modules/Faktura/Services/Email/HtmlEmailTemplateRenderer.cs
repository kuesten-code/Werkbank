using Kuestencode.Core.Enums;
using Kuestencode.Core.Models;
using Kuestencode.Faktura.Models;
using System.Globalization;

namespace Kuestencode.Faktura.Services.Email;

/// <summary>
/// Renders HTML email templates for invoices
/// </summary>
public class HtmlEmailTemplateRenderer : IEmailTemplateRenderer
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<HtmlEmailTemplateRenderer> _logger;

    public HtmlEmailTemplateRenderer(
        IWebHostEnvironment environment,
        ILogger<HtmlEmailTemplateRenderer> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public string RenderHtmlBody(Invoice invoice, Company company, string? customMessage)
    {
        var culture = new CultureInfo("de-DE");
        var paymentAmount = invoice.TotalDownPayments > 0 ? invoice.AmountDue : invoice.TotalGross;
        var formattedTotal = paymentAmount.ToString("C", culture);
        var formattedDate = invoice.InvoiceDate.ToString("dd.MM.yyyy", culture);
        var formattedDueDate = invoice.DueDate?.ToString("dd.MM.yyyy", culture) ?? "Sofort fällig";

        var templateName = company.EmailLayout switch
        {
            EmailLayout.Klar => "EmailTemplateKlar.html",
            EmailLayout.Strukturiert => "EmailTemplateStrukturiert.html",
            EmailLayout.Betont => "EmailTemplateBetont.html",
            _ => "EmailTemplateKlar.html"
        };

        var templatePath = Path.Combine(_environment.WebRootPath, "templates", templateName);

        if (!File.Exists(templatePath))
        {
            _logger.LogWarning("Email template {TemplateName} not found, using fallback", templateName);
            return BuildFallbackHtmlBody(invoice, company, customMessage);
        }

        var template = File.ReadAllText(templatePath);

        // Replace colors
        template = template.Replace("{{PRIMARY_COLOR}}", company.EmailPrimaryColor);
        template = template.Replace("{{ACCENT_COLOR}}", company.EmailAccentColor);

        // Prepare greeting and closing
        var greeting = string.IsNullOrWhiteSpace(company.EmailGreeting)
            ? "Sehr geehrte Damen und Herren,\n\nanbei erhalten Sie Ihre Rechnung."
            : company.EmailGreeting;

        var closing = string.IsNullOrWhiteSpace(company.EmailClosing)
            ? "Mit freundlichen Grüßen\n\n{{Firmenname}}"
            : company.EmailClosing;

        if (!string.IsNullOrWhiteSpace(customMessage))
        {
            greeting = $"{greeting}\n\n{customMessage}";
        }

        var firmenname = !string.IsNullOrEmpty(company.BusinessName)
            ? company.BusinessName
            : company.OwnerFullName;

        greeting = ReplacePlaceholders(greeting, invoice.InvoiceNumber, formattedDueDate, firmenname);
        closing = ReplacePlaceholders(closing, invoice.InvoiceNumber, formattedDueDate, firmenname);

        // Replace template placeholders
        template = template.Replace("{{GREETING}}", greeting.Replace("\n", "<br>"));
        template = template.Replace("{{CLOSING}}", closing.Replace("\n", "<br>"));
        template = template.Replace("{{INVOICE_NUMBER}}", invoice.InvoiceNumber);
        template = template.Replace("{{INVOICE_DATE}}", formattedDate);
        template = template.Replace("{{DUE_DATE}}", formattedDueDate);
        template = template.Replace("{{TOTAL_AMOUNT}}", formattedTotal);

        return template;
    }

    public string RenderPlainTextBody(Invoice invoice, Company company, string? customMessage)
    {
        var culture = new CultureInfo("de-DE");
        var paymentAmount = invoice.TotalDownPayments > 0 ? invoice.AmountDue : invoice.TotalGross;
        var formattedTotal = paymentAmount.ToString("C", culture);
        var formattedDate = invoice.InvoiceDate.ToString("dd.MM.yyyy", culture);
        var formattedDueDate = invoice.DueDate?.ToString("dd.MM.yyyy", culture) ?? "Sofort fällig";

        var text = $@"
{company.BusinessName ?? company.OwnerFullName}
{new string('=', (company.BusinessName ?? company.OwnerFullName).Length)}

Sehr geehrte Damen und Herren,

{(!string.IsNullOrWhiteSpace(customMessage) ? $"{customMessage}\n\n" : "")}anbei erhalten Sie die Rechnung {invoice.InvoiceNumber}.

RECHNUNGSDETAILS:
------------------
Rechnungsbetrag: {formattedTotal}
{(invoice.DiscountAmount > 0 ? $"  (inkl. Rabatt: {(invoice.DiscountType == DiscountType.Percentage ? $"{invoice.DiscountValue}%" : invoice.DiscountAmount.ToString("C", culture))})\n" : "")}Rechnungsnummer: {invoice.InvoiceNumber}
Rechnungsdatum:  {formattedDate}
Fällig am:       {formattedDueDate}

BANKVERBINDUNG:
---------------
Kontoinhaber:    {company.AccountHolder ?? company.OwnerFullName}
Bank:            {company.BankName}
IBAN:            {company.BankAccount}
{(!string.IsNullOrWhiteSpace(company.Bic) ? $"BIC:             {company.Bic}\n" : "")}Verwendungszweck: {invoice.InvoiceNumber}

Die Rechnung finden Sie im Anhang dieser E-Mail als PDF-Datei.

{(!string.IsNullOrWhiteSpace(company.EmailSignature) ? company.EmailSignature : "Vielen Dank für Ihren Auftrag!")}

--
{company.BusinessName ?? company.OwnerFullName}
{company.Address}
{company.PostalCode} {company.City}
{(!string.IsNullOrWhiteSpace(company.Phone) ? $"Tel: {company.Phone}\n" : "")}{(!string.IsNullOrWhiteSpace(company.Email) ? $"E-Mail: {company.Email}\n" : "")}{(!string.IsNullOrWhiteSpace(company.Website) ? $"Web: {company.Website}\n" : "")}{(!string.IsNullOrWhiteSpace(company.TaxNumber) ? $"Steuernummer: {company.TaxNumber}\n" : "")}{(!string.IsNullOrWhiteSpace(company.VatId) ? $"USt-IdNr: {company.VatId}\n" : "")}";

        return text.Trim();
    }

    private string ReplacePlaceholders(string text, string invoiceNumber, string dueDate, string firmenname)
    {
        return text
            .Replace("{{Firmenname}}", firmenname)
            .Replace("{{Rechnungsnummer}}", invoiceNumber)
            .Replace("{{Faelligkeitsdatum}}", dueDate);
    }

    private string BuildFallbackHtmlBody(Invoice invoice, Company company, string? customMessage)
    {
        var culture = new CultureInfo("de-DE");
        var paymentAmount = invoice.TotalDownPayments > 0 ? invoice.AmountDue : invoice.TotalGross;
        var formattedTotal = paymentAmount.ToString("C", culture);
        var formattedDate = invoice.InvoiceDate.ToString("dd.MM.yyyy", culture);
        var formattedDueDate = invoice.DueDate?.ToString("dd.MM.yyyy", culture) ?? "Sofort fällig";

        var html = $@"
<!DOCTYPE html>
<html lang=""de"">
<head>
    <meta charset=""UTF-8"">
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #0F2A3D; color: white; padding: 20px; text-align: center; }}
        .content {{ background-color: #f8f9fa; padding: 20px; margin: 20px 0; }}
        .details {{ background-color: white; padding: 15px; margin: 15px 0; border-left: 3px solid #3FA796; }}
        .footer {{ text-align: center; color: #666; font-size: 12px; margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; }}
        .highlight {{ color: #0F2A3D; font-weight: bold; }}
        .bank-details {{ background-color: #fff; padding: 15px; margin: 15px 0; border: 1px solid #ddd; }}
        .signature {{ margin-top: 20px; white-space: pre-line; }}
        table {{ width: 100%; border-collapse: collapse; }}
        td {{ padding: 8px 0; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>{company.BusinessName ?? company.OwnerFullName}</h1>
        </div>

        <div class=""content"">
            <p>Sehr geehrte Damen und Herren,</p>

            {(!string.IsNullOrWhiteSpace(customMessage) ? $"<p>{customMessage.Replace("\n", "<br>")}</p>" : "")}

            <p>anbei erhalten Sie die Rechnung <span class=""highlight"">{invoice.InvoiceNumber}</span>.</p>

            <div class=""details"">
                <table>
                    <tr>
                        <td><strong>Rechnungsbetrag:</strong></td>
                        <td class=""highlight"">{formattedTotal}</td>
                    </tr>
                    {(invoice.DiscountAmount > 0 ? $@"
                    <tr>
                        <td colspan=""2"" style=""font-size: 12px; color: #666;"">
                            <em>inkl. Rabatt: {(invoice.DiscountType == DiscountType.Percentage ? $"{invoice.DiscountValue}%" : invoice.DiscountAmount.ToString("C", culture))}</em>
                        </td>
                    </tr>" : "")}
                    <tr>
                        <td><strong>Rechnungsnummer:</strong></td>
                        <td>{invoice.InvoiceNumber}</td>
                    </tr>
                    <tr>
                        <td><strong>Rechnungsdatum:</strong></td>
                        <td>{formattedDate}</td>
                    </tr>
                    <tr>
                        <td><strong>Fällig am:</strong></td>
                        <td>{formattedDueDate}</td>
                    </tr>
                </table>
            </div>

            <div class=""bank-details"">
                <h3 style=""margin-top: 0;"">Bankverbindung</h3>
                <table>
                    <tr>
                        <td><strong>Kontoinhaber:</strong></td>
                        <td>{company.AccountHolder ?? company.OwnerFullName}</td>
                    </tr>
                    <tr>
                        <td><strong>Bank:</strong></td>
                        <td>{company.BankName}</td>
                    </tr>
                    <tr>
                        <td><strong>IBAN:</strong></td>
                        <td>{company.BankAccount}</td>
                    </tr>
                    {(!string.IsNullOrWhiteSpace(company.Bic) ? $@"
                    <tr>
                        <td><strong>BIC:</strong></td>
                        <td>{company.Bic}</td>
                    </tr>" : "")}
                    <tr>
                        <td><strong>Verwendungszweck:</strong></td>
                        <td>{invoice.InvoiceNumber}</td>
                    </tr>
                </table>
            </div>

            <p>Die Rechnung finden Sie im Anhang dieser E-Mail als PDF-Datei.</p>

            {(!string.IsNullOrWhiteSpace(company.EmailSignature) ? $"<div class=\"signature\">{company.EmailSignature}</div>" : "<p>Vielen Dank für Ihren Auftrag!</p>")}
        </div>

        <div class=""footer"">
            <p><strong>{company.BusinessName ?? company.OwnerFullName}</strong></p>
            <p>{company.Address}, {company.PostalCode} {company.City}</p>
            {(!string.IsNullOrWhiteSpace(company.Phone) ? $"<p>Tel: {company.Phone}</p>" : "")}
            {(!string.IsNullOrWhiteSpace(company.Email) ? $"<p>E-Mail: {company.Email}</p>" : "")}
            {(!string.IsNullOrWhiteSpace(company.Website) ? $"<p>Web: {company.Website}</p>" : "")}
            {(!string.IsNullOrWhiteSpace(company.TaxNumber) ? $"<p>Steuernummer: {company.TaxNumber}</p>" : "")}
            {(!string.IsNullOrWhiteSpace(company.VatId) ? $"<p>USt-IdNr: {company.VatId}</p>" : "")}
        </div>
    </div>
</body>
</html>";

        return html;
    }
}
