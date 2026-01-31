namespace Kuestencode.Shared.Pdf.Core;

/// <summary>
/// Metadaten für ein Dokument (Rechnung, Angebot, etc.)
/// </summary>
public class PdfDocumentInfo
{
    /// <summary>Dokumenttyp-Bezeichnung (z.B. "Rechnung", "Angebot")</summary>
    public required string DocumentType { get; init; }

    /// <summary>Dokumentnummer</summary>
    public required string DocumentNumber { get; init; }

    /// <summary>Erstelldatum</summary>
    public required DateTime DocumentDate { get; init; }

    /// <summary>Kundennummer</summary>
    public required string CustomerNumber { get; init; }

    /// <summary>Fälligkeitsdatum (bei Rechnungen) oder Gültig-bis (bei Angeboten)</summary>
    public DateTime? DueDate { get; init; }

    /// <summary>Bezeichnung für DueDate (z.B. "Fällig:" oder "Gültig bis:")</summary>
    public string DueDateLabel { get; init; } = "Fällig:";

    /// <summary>Referenz/Betreff (optional)</summary>
    public string? Reference { get; init; }

    /// <summary>Leistungszeitraum Start (optional)</summary>
    public DateTime? ServicePeriodStart { get; init; }

    /// <summary>Leistungszeitraum Ende (optional)</summary>
    public DateTime? ServicePeriodEnd { get; init; }
}
