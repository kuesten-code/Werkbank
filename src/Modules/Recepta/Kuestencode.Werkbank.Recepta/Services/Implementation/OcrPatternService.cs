using System.Globalization;
using System.Text.RegularExpressions;
using Kuestencode.Werkbank.Recepta.Data.Repositories;
using Kuestencode.Werkbank.Recepta.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Kuestencode.Werkbank.Recepta.Services;

/// <summary>
/// Selbstlernendes OCR-Pattern-System.
/// Lernt aus Benutzerkorrekturen und extrahiert Felder aus OCR-Texten.
/// </summary>
public class OcrPatternService : IOcrPatternService
{
    private readonly ISupplierOcrPatternRepository _patternRepository;
    private readonly ILogger<OcrPatternService> _logger;

    private const int ContextLength = 20;

    /// <summary>
    /// Generische deutsche Patterns als Fallback, wenn keine lieferantenspezifischen vorhanden sind.
    /// Tupel: (Feldname, Regex-Pattern mit benannter Gruppe "value")
    /// </summary>
    private static readonly List<(string FieldName, string Pattern)> GenericPatterns =
    [
        // Bruttobetrag
        ("AmountGross", @"(?:Gesamtbetrag|Summe|Brutto|Endbetrag|Rechnungsbetrag|Gesamtsumme)\s*:?\s*(?<value>[\d.,]+)\s*(?:EUR|€)?"),
        // Nettobetrag
        ("AmountNet", @"(?:Netto|Nettobetrag|Nettosumme)\s*:?\s*(?<value>[\d.,]+)\s*(?:EUR|€)?"),
        // Steuersatz
        ("TaxRate", @"(?<value>(?:19|7))\s*[%,]"),
        // Rechnungsnummer
        ("InvoiceNumber", @"(?:Rechnungsnr\.?|Re[\-\.]?Nr\.?|Rechnung\s*Nr\.?|Rechnungsnummer|Invoice\s*No\.?)\s*:?\s*(?<value>[A-Za-z0-9\-/]+)"),
        // Rechnungsdatum
        ("InvoiceDate", @"(?:Rechnungsdatum|Datum|Date)\s*:?\s*(?<value>\d{1,2}[./\-]\d{1,2}[./\-]\d{2,4})"),
        // IBAN
        ("IBAN", @"(?<value>DE\d{20})")
    ];

    /// <summary>
    /// Deutsche Datumsformate für die Erkennung.
    /// </summary>
    private static readonly string[] DateFormats =
    [
        "dd.MM.yyyy",
        "d.M.yyyy",
        "dd.MM.yy",
        "d.M.yy",
        "dd/MM/yyyy",
        "d/M/yyyy",
        "dd-MM-yyyy",
        "d-M-yyyy",
        "yyyy-MM-dd"
    ];

    public OcrPatternService(
        ISupplierOcrPatternRepository patternRepository,
        ILogger<OcrPatternService> logger)
    {
        _patternRepository = patternRepository;
        _logger = logger;
    }

    public async Task LearnPatternAsync(Guid supplierId, string fieldName, string ocrText, string userValue)
    {
        if (string.IsNullOrWhiteSpace(ocrText) || string.IsNullOrWhiteSpace(userValue))
            return;

        var normalizedOcrText = NormalizeWhitespace(ocrText);

        // Versuche den Wert im OCR-Text zu finden (verschiedene Formate)
        var position = FindValuePosition(normalizedOcrText, userValue, fieldName);
        if (position < 0)
        {
            _logger.LogDebug(
                "Konnte Wert '{UserValue}' für Feld '{FieldName}' nicht im OCR-Text finden",
                userValue, fieldName);
            return;
        }

        // Kontext vor dem Wert extrahieren (20 Zeichen)
        var contextStart = Math.Max(0, position - ContextLength);
        var contextText = normalizedOcrText[contextStart..position].TrimStart();

        // Pattern aus Kontext erstellen: Whitespace normalisieren zu \s*
        var pattern = Regex.Escape(contextText);
        pattern = Regex.Replace(pattern, @"(\\ )+", @"\s*");
        pattern += @"\s*";

        _logger.LogInformation(
            "Gelerntes Pattern für Lieferant {SupplierId}, Feld '{FieldName}': {Pattern}",
            supplierId, fieldName, pattern);

        // Bestehendes Pattern überschreiben oder neues anlegen
        var existing = await _patternRepository.GetBySupplierIdAndFieldNameAsync(supplierId, fieldName);
        if (existing != null)
        {
            existing.Pattern = pattern;
            await _patternRepository.UpdateAsync(existing);
        }
        else
        {
            var newPattern = new SupplierOcrPattern
            {
                Id = Guid.NewGuid(),
                SupplierId = supplierId,
                FieldName = fieldName,
                Pattern = pattern
            };
            await _patternRepository.AddAsync(newPattern);
        }
    }

    public async Task<Dictionary<string, string>> ExtractFieldsAsync(Guid? supplierId, string ocrText)
    {
        var result = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(ocrText))
            return result;

        var normalizedText = NormalizeWhitespace(ocrText);

        // Lieferant erkannt → NUR dessen Patterns verwenden
        if (supplierId.HasValue)
        {
            var patterns = await _patternRepository.GetBySupplerIdAsync(supplierId.Value);

            foreach (var pattern in patterns)
            {
                try
                {
                    var extracted = ApplySupplierPattern(normalizedText, pattern);
                    if (extracted != null)
                    {
                        result[pattern.FieldName] = NormalizeExtractedValue(pattern.FieldName, extracted);
                        _logger.LogDebug(
                            "Lieferanten-Pattern für '{FieldName}' ergab: {Value}",
                            pattern.FieldName, result[pattern.FieldName]);
                    }
                }
                catch (RegexMatchTimeoutException)
                {
                    _logger.LogWarning(
                        "Timeout bei Pattern für Feld '{FieldName}', Lieferant {SupplierId}",
                        pattern.FieldName, supplierId);
                }
            }

            // Keine generischen Patterns mischen — nur lieferantenspezifische Ergebnisse
            return result;
        }

        // Kein Lieferant erkannt → generische Patterns als Ersthilfe
        foreach (var (fieldName, genericPattern) in GenericPatterns)
        {
            try
            {
                var match = Regex.Match(
                    normalizedText, genericPattern,
                    RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2));

                if (match.Success && match.Groups["value"].Success)
                {
                    var raw = match.Groups["value"].Value;
                    result[fieldName] = NormalizeExtractedValue(fieldName, raw);
                    _logger.LogDebug(
                        "Generisches Pattern für '{FieldName}' ergab: {Value}",
                        fieldName, result[fieldName]);
                }
            }
            catch (RegexMatchTimeoutException)
            {
                _logger.LogWarning("Timeout bei generischem Pattern für Feld '{FieldName}'", fieldName);
            }
        }

        return result;
    }

    /// <summary>
    /// Wendet ein lieferantenspezifisches Pattern an und gibt den Rohwert zurück.
    /// Das Pattern beschreibt den Kontext VOR dem Wert.
    /// Nach dem Pattern wird der nächste "Wert-Block" extrahiert.
    /// </summary>
    private static string? ApplySupplierPattern(string text, SupplierOcrPattern pattern)
    {
        // Pattern + beliebiger Wert danach (Ziffern, Buchstaben, Punkte, Kommas, Bindestriche, Schrägstriche)
        var fullPattern = pattern.Pattern + @"(?<value>[A-Za-z0-9.,\-/]+)";

        var match = Regex.Match(text, fullPattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(2));
        if (match.Success && match.Groups["value"].Success)
        {
            return match.Groups["value"].Value;
        }

        return null;
    }

    /// <summary>
    /// Findet die Position eines Benutzerwertes im OCR-Text.
    /// Probiert verschiedene Darstellungsformen des Wertes.
    /// </summary>
    private static int FindValuePosition(string ocrText, string userValue, string fieldName)
    {
        // Direkte Suche
        var pos = ocrText.IndexOf(userValue, StringComparison.OrdinalIgnoreCase);
        if (pos >= 0) return pos;

        // Für Beträge: normalisiertes Format ↔ deutsches Format probieren
        if (IsAmountField(fieldName))
        {
            var germanFormat = ToGermanAmount(userValue);
            if (germanFormat != null)
            {
                pos = ocrText.IndexOf(germanFormat, StringComparison.OrdinalIgnoreCase);
                if (pos >= 0) return pos;
            }

            // Auch ohne Tausender-Trennzeichen suchen
            var simpleGerman = ToSimpleGermanAmount(userValue);
            if (simpleGerman != null)
            {
                pos = ocrText.IndexOf(simpleGerman, StringComparison.OrdinalIgnoreCase);
                if (pos >= 0) return pos;
            }
        }

        // Für Datum: verschiedene Formate probieren
        if (fieldName.Equals("InvoiceDate", StringComparison.OrdinalIgnoreCase) &&
            DateOnly.TryParse(userValue, CultureInfo.InvariantCulture, out var date))
        {
            foreach (var format in DateFormats)
            {
                var formatted = date.ToString(format, CultureInfo.InvariantCulture);
                pos = ocrText.IndexOf(formatted, StringComparison.OrdinalIgnoreCase);
                if (pos >= 0) return pos;
            }
        }

        return -1;
    }

    /// <summary>
    /// Normalisiert einen extrahierten Wert je nach Feldtyp.
    /// </summary>
    private static string NormalizeExtractedValue(string fieldName, string rawValue)
    {
        if (IsAmountField(fieldName))
        {
            return NormalizeAmount(rawValue);
        }

        if (fieldName.Equals("InvoiceDate", StringComparison.OrdinalIgnoreCase))
        {
            return NormalizeDate(rawValue);
        }

        if (fieldName.Equals("TaxRate", StringComparison.OrdinalIgnoreCase))
        {
            return NormalizeAmount(rawValue);
        }

        return rawValue.Trim();
    }

    /// <summary>
    /// Normalisiert einen deutschen Betrag zu einem invarianten Dezimalwert.
    /// "1.234,56" → "1234.56"
    /// "1234,56"  → "1234.56"
    /// "1234.56"  → "1234.56" (schon normalisiert)
    /// </summary>
    private static string NormalizeAmount(string value)
    {
        var cleaned = value.Trim();

        // Deutsches Format: Punkt als Tausender, Komma als Dezimal
        if (cleaned.Contains(','))
        {
            cleaned = cleaned.Replace(".", ""); // Tausender-Punkte entfernen
            cleaned = cleaned.Replace(",", "."); // Dezimalkomma → Punkt
        }

        if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
        {
            return amount.ToString(CultureInfo.InvariantCulture);
        }

        return value.Trim();
    }

    /// <summary>
    /// Normalisiert ein deutsches Datum zu ISO-Format (yyyy-MM-dd).
    /// </summary>
    private static string NormalizeDate(string value)
    {
        foreach (var format in DateFormats)
        {
            if (DateOnly.TryParseExact(value.Trim(), format, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var date))
            {
                return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
        }

        return value.Trim();
    }

    /// <summary>
    /// Konvertiert einen normalisierten Betrag "1234.56" ins deutsche Format "1.234,56".
    /// </summary>
    private static string? ToGermanAmount(string normalizedValue)
    {
        if (decimal.TryParse(normalizedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
        {
            return amount.ToString("N2", new CultureInfo("de-DE"));
        }
        return null;
    }

    /// <summary>
    /// Konvertiert einen normalisierten Betrag "1234.56" ins einfache deutsche Format "1234,56" (ohne Tausender).
    /// </summary>
    private static string? ToSimpleGermanAmount(string normalizedValue)
    {
        if (decimal.TryParse(normalizedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
        {
            return amount.ToString("0.00", CultureInfo.InvariantCulture).Replace(".", ",");
        }
        return null;
    }

    private static bool IsAmountField(string fieldName)
    {
        return fieldName is "AmountGross" or "AmountNet" or "AmountTax" or "TaxRate";
    }

    private static string NormalizeWhitespace(string text)
    {
        return Regex.Replace(text, @"\s+", " ");
    }
}
