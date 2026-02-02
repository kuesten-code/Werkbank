using Kuestencode.Core.Models;
using Kuestencode.Shared.Pdf.Core;
using System.Globalization;

// Note: PdfDocumentInfo is used instead of DocumentMetadata to avoid conflict with QuestPDF.Infrastructure.DocumentMetadata

namespace Kuestencode.Shared.Pdf.Components;

/// <summary>
/// Ersetzt Platzhalter in Template-Texten mit tatsächlichen Daten.
/// </summary>
public class PdfTemplateEngine
{
    private readonly CultureInfo _germanCulture = new("de-DE");

    /// <summary>
    /// Ersetzt Platzhalter für Rechnungen.
    /// </summary>
    /// <param name="text">Template-Text mit Platzhaltern</param>
    /// <param name="metadata">Dokumentmetadaten</param>
    /// <param name="summary">Zusammenfassung der Beträge</param>
    /// <param name="company">Firmenstammdaten</param>
    /// <param name="customer">Kundendaten</param>
    /// <returns>Text mit ersetzten Platzhaltern</returns>
    public string ReplacePlaceholders(
        string text,
        PdfDocumentInfo metadata,
        DocumentSummary summary,
        Company company,
        Customer customer)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var firmenname = !string.IsNullOrEmpty(company.BusinessName)
            ? company.BusinessName
            : company.OwnerFullName;

        return text
            // Firmendaten
            .Replace("{{Firmenname}}", firmenname)

            // Kundendaten
            .Replace("{{Kundenname}}", customer.Name)

            // Dokumentdaten
            .Replace("{{Dokumentnummer}}", metadata.DocumentNumber)
            .Replace("{{Dokumentdatum}}", metadata.DocumentDate.ToString("dd.MM.yyyy", _germanCulture))
            .Replace("{{Gueltigkeitsdatum}}", metadata.DueDate?.ToString("dd.MM.yyyy", _germanCulture) ?? "")
            .Replace("{{Faelligkeitsdatum}}", metadata.DueDate?.ToString("dd.MM.yyyy", _germanCulture) ?? "")

            // Rechnungsspezifisch (für Abwärtskompatibilität)
            .Replace("{{Rechnungsnummer}}", metadata.DocumentNumber)
            .Replace("{{Rechnungsdatum}}", metadata.DocumentDate.ToString("dd.MM.yyyy", _germanCulture))
            .Replace("{{Rechnungsbetrag}}", summary.TotalGross.ToString("C2", _germanCulture))

            // Angebotsspezifisch
            .Replace("{{Angebotsnummer}}", metadata.DocumentNumber)
            .Replace("{{Angebotsdatum}}", metadata.DocumentDate.ToString("dd.MM.yyyy", _germanCulture))
            .Replace("{{Angebotsbetrag}}", summary.TotalGross.ToString("C2", _germanCulture))

            // Beträge
            .Replace("{{Bruttosumme}}", summary.TotalGross.ToString("C2", _germanCulture))
            .Replace("{{Nettosumme}}", summary.TotalNet.ToString("C2", _germanCulture))
            .Replace("{{ZuZahlen}}", summary.AmountDue.ToString("C2", _germanCulture));
    }

    /// <summary>
    /// Ersetzt einfache Platzhalter nur mit Firmendaten.
    /// </summary>
    public string ReplaceCompanyPlaceholders(string text, Company company)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var firmenname = !string.IsNullOrEmpty(company.BusinessName)
            ? company.BusinessName
            : company.OwnerFullName;

        return text.Replace("{{Firmenname}}", firmenname);
    }
}
