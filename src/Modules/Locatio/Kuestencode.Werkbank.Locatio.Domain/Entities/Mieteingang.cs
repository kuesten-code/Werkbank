namespace Kuestencode.Werkbank.Locatio.Domain.Entities;

/// <summary>
/// Tatsächlich eingegangene Miete. Maßgeblich für Saldo ist das Zahlungsdatum (Zuflussprinzip).
/// Der USt-Satz wird beim Erfassen aus der Einheit übernommen, damit spätere Vertragsänderungen
/// bereits erfasste Zahlungen nicht verändern.
/// </summary>
public class Mieteingang
{
    public Guid Id { get; set; }
    public Guid MieteinheitId { get; set; }
    public Mieteinheit? Mieteinheit { get; set; }

    public DateOnly Zahlungsdatum { get; set; }

    /// <summary>Erster Tag des Monats, für den die Miete gezahlt wurde.</summary>
    public DateOnly Monat { get; set; }

    public decimal Kaltmiete { get; set; }
    public decimal Nebenkosten { get; set; }
    public decimal UstSatz { get; set; }
    public string? Notiz { get; set; }

    public DateTime CreatedAt { get; set; }

    public decimal Netto => Kaltmiete + Nebenkosten;
    public decimal Ust => Math.Round(Netto * UstSatz / 100m, 2);
    public decimal Brutto => Netto + Ust;
}
