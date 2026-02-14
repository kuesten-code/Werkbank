using Kuestencode.Werkbank.Recepta.Domain.Dtos;

namespace Kuestencode.Werkbank.Recepta.Services;

/// <summary>
/// Service zum Parsen von XRechnung/ZUGFeRD-Rechnungen.
/// Unterstützt XRechnung (UBL 2.1), ZUGFeRD 1.x/2.x und Factur-X.
/// </summary>
public interface IXRechnungService
{
    /// <summary>
    /// Prüft ob die Datei als XRechnung/ZUGFeRD verarbeitet werden kann.
    /// Bei XML: Prüft ob es ein valides Rechnungsformat ist.
    /// Bei PDF: Prüft ob eingebettete ZUGFeRD-XML-Daten vorhanden sind.
    /// Stream-Position wird nach der Prüfung zurückgesetzt.
    /// </summary>
    bool CanProcess(Stream file, string fileName);

    /// <summary>
    /// Parst eine XRechnung/ZUGFeRD-Datei und extrahiert strukturierte Rechnungsdaten.
    /// </summary>
    Task<XRechnungData> ParseAsync(Stream file, string fileName);
}
