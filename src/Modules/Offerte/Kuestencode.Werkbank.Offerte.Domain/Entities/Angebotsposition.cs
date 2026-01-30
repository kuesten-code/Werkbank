using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuestencode.Werkbank.Offerte.Domain.Entities;

/// <summary>
/// Eine einzelne Position innerhalb eines Angebots.
/// </summary>
public class Angebotsposition
{
    public Guid Id { get; set; }

    [Required]
    public Guid AngebotId { get; set; }

    /// <summary>
    /// Reihenfolge der Position im Angebot (1, 2, 3, ...).
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Beschreibungstext der Position.
    /// </summary>
    [Required(ErrorMessage = "Text ist erforderlich")]
    [MaxLength(1000)]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Menge (z.B. Stunden, Stück).
    /// </summary>
    [Required(ErrorMessage = "Menge ist erforderlich")]
    [Column(TypeName = "decimal(18,3)")]
    public decimal Menge { get; set; }

    /// <summary>
    /// Preis pro Einheit in Euro.
    /// </summary>
    [Required(ErrorMessage = "Einzelpreis ist erforderlich")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Einzelpreis { get; set; }

    /// <summary>
    /// Steuersatz in Prozent (z.B. 19.0 für 19%).
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal Steuersatz { get; set; } = 19.0m;

    /// <summary>
    /// Optionaler Rabatt in Prozent (z.B. 10.0 für 10%).
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? Rabatt { get; set; }

    // Navigation Properties
    public Angebot Angebot { get; set; } = null!;

    // Berechnete Eigenschaften

    /// <summary>
    /// Nettosumme der Position (Menge × Einzelpreis - Rabatt).
    /// </summary>
    [NotMapped]
    public decimal Nettosumme
    {
        get
        {
            var basis = Math.Round(Menge * Einzelpreis, 2);
            if (Rabatt.HasValue && Rabatt.Value > 0)
            {
                var rabattBetrag = Math.Round(basis * Rabatt.Value / 100, 2);
                return basis - rabattBetrag;
            }
            return basis;
        }
    }

    /// <summary>
    /// Steuerbetrag der Position.
    /// </summary>
    [NotMapped]
    public decimal Steuerbetrag => Math.Round(Nettosumme * Steuersatz / 100, 2);

    /// <summary>
    /// Bruttosumme der Position (Netto + Steuer).
    /// </summary>
    [NotMapped]
    public decimal Bruttosumme => Nettosumme + Steuerbetrag;
}
