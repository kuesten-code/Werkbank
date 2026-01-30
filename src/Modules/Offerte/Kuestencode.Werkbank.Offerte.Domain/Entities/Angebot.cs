using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Kuestencode.Werkbank.Offerte.Domain.Enums;

namespace Kuestencode.Werkbank.Offerte.Domain.Entities;

/// <summary>
/// Ein Angebot an einen Kunden.
/// </summary>
public class Angebot
{
    public Guid Id { get; set; }

    /// <summary>
    /// Eindeutige Angebotsnummer (eigener Nummernkreis).
    /// </summary>
    [Required(ErrorMessage = "Angebotsnummer ist erforderlich")]
    [MaxLength(20)]
    public string Angebotsnummer { get; set; } = string.Empty;

    /// <summary>
    /// Referenz zum Kunden im Host-Schema.
    /// </summary>
    [Required]
    public int KundeId { get; set; }

    /// <summary>
    /// Aktueller Status des Angebots.
    /// </summary>
    public AngebotStatus Status { get; set; } = AngebotStatus.Entwurf;

    /// <summary>
    /// Datum der Erstellung des Angebots.
    /// </summary>
    [Required]
    public DateTime Erstelldatum { get; set; }

    /// <summary>
    /// Datum, bis zu dem das Angebot gültig ist.
    /// </summary>
    [Required]
    public DateTime GueltigBis { get; set; }

    /// <summary>
    /// Optionale Referenz (z.B. Projekt, Auftrag, Bestellnummer).
    /// </summary>
    [MaxLength(100)]
    public string? Referenz { get; set; }

    /// <summary>
    /// Optionale Notizen/Bemerkungen zum Angebot.
    /// </summary>
    [MaxLength(2000)]
    public string? Bemerkungen { get; set; }

    /// <summary>
    /// Optionaler einleitender Text vor den Positionen.
    /// </summary>
    [MaxLength(2000)]
    public string? Einleitung { get; set; }

    /// <summary>
    /// Optionaler abschließender Text nach den Positionen.
    /// </summary>
    [MaxLength(2000)]
    public string? Schlusstext { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Status-Tracking
    public DateTime? VersendetAm { get; set; }
    public DateTime? AngenommenAm { get; set; }
    public DateTime? AbgelehntAm { get; set; }
    public DateTime? AbgelaufenAm { get; set; }

    // E-Mail-Tracking
    public DateTime? EmailGesendetAm { get; set; }

    [MaxLength(200)]
    public string? EmailGesendetAn { get; set; }

    public int EmailAnzahl { get; set; } = 0;

    // Druck-Tracking
    public DateTime? GedrucktAm { get; set; }
    public int DruckAnzahl { get; set; } = 0;

    // Navigation Properties
    public List<Angebotsposition> Positionen { get; set; } = new();

    // Berechnete Eigenschaften

    /// <summary>
    /// Summe aller Nettowerte der Positionen.
    /// </summary>
    [NotMapped]
    public decimal Nettosumme => Positionen.Sum(p => p.Nettosumme);

    /// <summary>
    /// Summe aller Steuerbeträge der Positionen.
    /// </summary>
    [NotMapped]
    public decimal Steuersumme => Positionen.Sum(p => p.Steuerbetrag);

    /// <summary>
    /// Gesamtsumme (Netto + Steuer).
    /// </summary>
    [NotMapped]
    public decimal Bruttosumme => Nettosumme + Steuersumme;

    /// <summary>
    /// Prüft, ob das Angebot in einem terminalen Status ist.
    /// </summary>
    [NotMapped]
    public bool IstTerminal => Status is AngebotStatus.Angenommen
                                      or AngebotStatus.Abgelehnt
                                      or AngebotStatus.Abgelaufen;

    /// <summary>
    /// Prüft, ob das Angebot bearbeitet werden kann.
    /// </summary>
    [NotMapped]
    public bool IstBearbeitbar => Status == AngebotStatus.Entwurf;

    /// <summary>
    /// Prüft, ob das Gültigkeitsdatum überschritten ist.
    /// </summary>
    [NotMapped]
    public bool IstAbgelaufen => DateTime.UtcNow.Date > GueltigBis.Date;
}
