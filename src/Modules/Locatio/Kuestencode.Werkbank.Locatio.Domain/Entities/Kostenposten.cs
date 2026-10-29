namespace Kuestencode.Werkbank.Locatio.Domain.Entities;

/// <summary>
/// Ausgabe zu einem Gebäude oder einer Einheit. Nur <see cref="Betrag"/> (der tatsächlich
/// getragene Anteil) geht an Saldo; Gesamtbetrag und Verteilschlüssel dienen der Dokumentation.
/// </summary>
public class Kostenposten
{
    public Guid Id { get; set; }

    public Guid? GebaeudeId { get; set; }
    public Gebaeude? Gebaeude { get; set; }

    public Guid? MieteinheitId { get; set; }
    public Mieteinheit? Mieteinheit { get; set; }

    public Guid KategorieId { get; set; }
    public Kostenkategorie? Kategorie { get; set; }

    public decimal Betrag { get; set; }
    public decimal? Gesamtbetrag { get; set; }
    public string? Verteilschluessel { get; set; }

    public DateOnly Zahlungsdatum { get; set; }
    public string Beschreibung { get; set; } = string.Empty;
    public string? Notiz { get; set; }

    public Guid? BelegId { get; set; }
    public Dokument? Beleg { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
