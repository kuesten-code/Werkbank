namespace Kuestencode.Werkbank.Recepta.Domain.Enums;

/// <summary>
/// Kategorie eines Belegs.
/// </summary>
public enum DocumentCategory
{
    /// <summary>
    /// Materialkosten.
    /// </summary>
    Material = 0,

    /// <summary>
    /// Subunternehmer / Fremdleistungen.
    /// </summary>
    Subcontractor = 1,

    /// <summary>
    /// Büromaterial / Bürokosten.
    /// </summary>
    Office = 2,

    /// <summary>
    /// Reisekosten.
    /// </summary>
    Travel = 3,

    /// <summary>
    /// Sonstiges.
    /// </summary>
    Other = 4
}
