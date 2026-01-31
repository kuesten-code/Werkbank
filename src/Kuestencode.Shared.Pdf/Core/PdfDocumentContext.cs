using Kuestencode.Core.Models;

namespace Kuestencode.Shared.Pdf.Core;

/// <summary>
/// Kontext-Objekt mit allen gemeinsamen Daten für die PDF-Generierung.
/// </summary>
public class PdfDocumentContext
{
    /// <summary>Firmenstammdaten</summary>
    public required Company Company { get; init; }

    /// <summary>Kundendaten</summary>
    public required Customer Customer { get; init; }

    /// <summary>Primary Color für das Layout</summary>
    public string PrimaryColor { get; init; } = Styling.PdfColors.DefaultPrimary;

    /// <summary>Accent Color für das Layout</summary>
    public string AccentColor { get; init; } = Styling.PdfColors.DefaultAccent;
}
