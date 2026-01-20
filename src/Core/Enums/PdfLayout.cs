namespace Kuestencode.Core.Enums;

/// <summary>
/// Layout-Stile für PDF-Dokumente.
/// </summary>
public enum PdfLayout
{
    Klar = 1,         // Default - minimalistisch, text-dominiert
    Strukturiert = 2, // Mit Boxen/Trennlinien für Übersichtlichkeit
    Betont = 3        // Farbiger Header, visuell stärker
}
