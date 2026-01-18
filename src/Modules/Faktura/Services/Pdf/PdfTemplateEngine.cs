using Kuestencode.Faktura.Models;
using System.Globalization;

namespace Kuestencode.Faktura.Services.Pdf;

/// <summary>
/// Handles replacement of placeholders in PDF template texts.
/// </summary>
public class PdfTemplateEngine
{
    private readonly CultureInfo _germanCulture = new CultureInfo("de-DE");

    /// <summary>
    /// Replaces placeholders in template text with actual invoice and company data.
    /// </summary>
    /// <param name="text">Template text containing placeholders</param>
    /// <param name="invoice">Invoice data</param>
    /// <param name="company">Company data</param>
    /// <returns>Text with replaced placeholders</returns>
    public string ReplacePlaceholders(string text, Invoice invoice, Company company)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var firmenname = !string.IsNullOrEmpty(company.BusinessName)
            ? company.BusinessName
            : company.OwnerFullName;

        return text
            .Replace("{{Firmenname}}", firmenname)
            .Replace("{{Rechnungsnummer}}", invoice.InvoiceNumber)
            .Replace("{{Rechnungsdatum}}", invoice.InvoiceDate.ToString("dd.MM.yyyy", _germanCulture))
            .Replace("{{Faelligkeitsdatum}}", invoice.DueDate?.ToString("dd.MM.yyyy", _germanCulture) ?? "")
            .Replace("{{Rechnungsbetrag}}", invoice.TotalGross.ToString("C2", _germanCulture))
            .Replace("{{Kundenname}}", invoice.Customer?.Name ?? "");
    }
}
