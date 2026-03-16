namespace Kuestencode.Werkbank.Saldo.Domain.Dtos;

public enum BuchungsTyp
{
    Einnahme,
    Ausgabe
}

/// <summary>
/// Repräsentiert eine einzelne Buchung (Einnahme oder Ausgabe) nach Zufluss-/Abflussprinzip.
/// </summary>
public class BuchungDto
{
    public Guid Id { get; set; }
    /// <summary>Quelle: "Faktura" oder "Recepta"</summary>
    public string Quelle { get; set; } = string.Empty;
    /// <summary>Rechnungs- oder Belegnummer aus dem Quellsystem</summary>
    public string QuelleId { get; set; } = string.Empty;
    /// <summary>Rechnungsdatum (nicht für EÜR relevant, nur zur Anzeige)</summary>
    public DateOnly BelegDatum { get; set; }
    /// <summary>Zahlungsdatum – maßgeblich für die EÜR (Zufluss-/Abflussprinzip)</summary>
    public DateOnly ZahlungsDatum { get; set; }
    /// <summary>Kundenname (Einnahme) oder Lieferantenname (Ausgabe)</summary>
    public string Beschreibung { get; set; } = string.Empty;
    public decimal Netto { get; set; }
    public decimal Ust { get; set; }
    public decimal Brutto { get; set; }
    public decimal UstSatz { get; set; }
    /// <summary>Nur bei Ausgaben: Kategorie des Belegs</summary>
    public string Kategorie { get; set; } = string.Empty;
    public string KontoNummer { get; set; } = string.Empty;
    public string KontoBezeichnung { get; set; } = string.Empty;
    public BuchungsTyp Typ { get; set; }
}

/// <summary>
/// Gesamtübersicht über Einnahmen, Ausgaben und Saldo für einen Zeitraum.
/// </summary>
public class SaldoUebersichtDto
{
    public DateOnly Von { get; set; }
    public DateOnly Bis { get; set; }
    public decimal Einnahmen { get; set; }
    public decimal Ausgaben { get; set; }
    public decimal Saldo => Einnahmen - Ausgaben;
    /// <summary>Umsatzsteuer aus Faktura-Rechnungen</summary>
    public decimal Umsatzsteuer { get; set; }
    /// <summary>Vorsteuer aus Recepta-Belegen</summary>
    public decimal Vorsteuer { get; set; }
    public decimal UstZahllast => Umsatzsteuer - Vorsteuer;
}

/// <summary>
/// Monatsweise USt-Übersicht für einen Zeitraum.
/// </summary>
public class UstUebersichtDto
{
    public List<UstMonatDto> Monate { get; set; } = new();
}

public class UstMonatDto
{
    public int Jahr { get; set; }
    public int Monat { get; set; }
    public decimal Umsatzsteuer19 { get; set; }
    public decimal Umsatzsteuer7 { get; set; }
    public decimal VorsteuerGesamt { get; set; }
    public decimal Zahllast => Umsatzsteuer19 + Umsatzsteuer7 - VorsteuerGesamt;
}
