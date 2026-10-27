namespace Kuestencode.Werkbank.Locatio.Domain.Entities;

/// <summary>
/// Ablage der externen Nebenkostenabrechnung je Einheit und Jahr. Keine eigene Abrechnungslogik.
/// </summary>
public class Nebenkostenabrechnung
{
    public Guid Id { get; set; }
    public Guid MieteinheitId { get; set; }
    public Mieteinheit? Mieteinheit { get; set; }

    public int Jahr { get; set; }

    /// <summary>Positiv = Nachzahlung des Mieters (Einnahme), negativ = Erstattung an den Mieter (Ausgabe).</summary>
    public decimal Ergebnis { get; set; }

    public DateOnly Zahlungsdatum { get; set; }

    public Guid? DokumentId { get; set; }
    public Dokument? Dokument { get; set; }

    public string? Notiz { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
