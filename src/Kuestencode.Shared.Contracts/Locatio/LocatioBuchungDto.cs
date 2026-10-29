namespace Kuestencode.Shared.Contracts.Locatio;

/// <summary>
/// Buchung aus der Vermietungsverwaltung für Saldo, nach Zahlungsdatum (Zufluss-/Abflussprinzip).
/// Locatio liefert nur steuerlich relevante Vorgänge: Tilgung und Veräußerungen sind nie enthalten.
/// </summary>
public class LocatioBuchungDto
{
    /// <summary>Id des Quelldatensatzes in Locatio.</summary>
    public Guid Id { get; set; }

    /// <summary>Lesbare Referenz, z. B. "Miete Whg 3 03/2026".</summary>
    public string Referenz { get; set; } = string.Empty;

    public DateOnly Zahlungsdatum { get; set; }
    public bool IstEinnahme { get; set; }
    public string Beschreibung { get; set; } = string.Empty;
    public decimal Netto { get; set; }
    public decimal Ust { get; set; }
    public decimal Brutto { get; set; }
    public decimal UstSatz { get; set; }
    public string Kategorie { get; set; } = string.Empty;

    /// <summary>Zielkonto aus der Locatio-Kategorie; leer, wenn dort keins hinterlegt ist.</summary>
    public string? KontoNummer { get; set; }
}
