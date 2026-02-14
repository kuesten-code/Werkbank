namespace Kuestencode.Werkbank.Recepta.Services;

/// <summary>
/// Service für selbstlernendes OCR-Pattern-Matching.
/// Lernt aus Benutzerkorrekturen und wendet Muster auf neue OCR-Texte an.
/// </summary>
public interface IOcrPatternService
{
    /// <summary>
    /// Lernt ein Pattern aus einem OCR-Text und dem vom Benutzer eingegebenen Wert.
    /// Extrahiert den Kontext vor dem Wert als Pattern.
    /// </summary>
    Task LearnPatternAsync(Guid supplierId, string fieldName, string ocrText, string userValue);

    /// <summary>
    /// Extrahiert Felder aus einem OCR-Text anhand gelernter Patterns und generischer Regeln.
    /// </summary>
    Task<Dictionary<string, string>> ExtractFieldsAsync(Guid? supplierId, string ocrText);
}
