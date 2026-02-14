namespace Kuestencode.Werkbank.Recepta.Services;

/// <summary>
/// Service für OCR-Texterkennung aus Dateien.
/// </summary>
public interface IOcrService
{
    /// <summary>
    /// Extrahiert Text aus einer Datei (PDF, JPG, PNG).
    /// Bei PDF wird die erste Seite konvertiert und dann OCR ausgeführt.
    /// </summary>
    /// <param name="fileStream">Der Dateiinhalt als Stream</param>
    /// <param name="fileName">Der Dateiname (zur Erkennung des Dateityps)</param>
    /// <returns>Der erkannte Text oder ein leerer String bei Fehlern</returns>
    Task<string> ExtractTextAsync(Stream fileStream, string fileName);
}
