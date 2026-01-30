namespace Kuestencode.Shared.Contracts.Offerte;

/// <summary>
/// DTO zur Übergabe von Angebotsdaten an das Faktura-Modul
/// für die Erstellung einer Rechnung.
/// </summary>
public class RechnungErstellungDto
{
    /// <summary>
    /// ID des Kunden (aus Host-Schema).
    /// </summary>
    public int KundeId { get; set; }

    /// <summary>
    /// Optionale Referenz (z.B. Angebotsnummer, Projektnummer).
    /// </summary>
    public string? Referenz { get; set; }

    /// <summary>
    /// Die zu übernehmenden Positionen.
    /// </summary>
    public List<RechnungspositionDto> Positionen { get; set; } = new();
}

/// <summary>
/// DTO für eine einzelne Rechnungsposition.
/// </summary>
public class RechnungspositionDto
{
    /// <summary>
    /// Beschreibungstext der Position.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Menge (z.B. Stunden, Stück).
    /// </summary>
    public decimal Menge { get; set; }

    /// <summary>
    /// Preis pro Einheit in Euro.
    /// </summary>
    public decimal Einzelpreis { get; set; }

    /// <summary>
    /// Steuersatz in Prozent (z.B. 19.0).
    /// </summary>
    public decimal Steuersatz { get; set; }

    /// <summary>
    /// Optionaler Rabatt in Prozent.
    /// </summary>
    public decimal? Rabatt { get; set; }
}
