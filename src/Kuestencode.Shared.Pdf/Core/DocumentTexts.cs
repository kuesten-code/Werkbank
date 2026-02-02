namespace Kuestencode.Shared.Pdf.Core;

/// <summary>
/// Texte für das Dokument (Einleitung, Schlusstext, etc.)
/// </summary>
public class DocumentTexts
{
    /// <summary>Anrede/Einleitungstext</summary>
    public string? Greeting { get; init; }

    /// <summary>Einleitungstext nach der Anrede</summary>
    public string? Introduction { get; init; }

    /// <summary>Schlusstext/Abschlussformel</summary>
    public string? ClosingText { get; init; }

    /// <summary>Gültigkeitshinweis (z.B. "Dieses Angebot ist gültig bis...")</summary>
    public string? ValidityNotice { get; init; }

    /// <summary>Zahlungshinweis (für Rechnungen)</summary>
    public string? PaymentNotice { get; init; }
}
