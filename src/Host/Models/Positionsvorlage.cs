using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kuestencode.Werkbank.Host.Models;

/// <summary>
/// Wiederverwendbare Rechnungs-/Angebotsposition, die Faktura und Offerte beim Erfassen vorschlagen.
/// </summary>
public class Positionsvorlage
{
    public int Id { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Text { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Einheit { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Einzelpreis { get; set; }

    /// <summary>
    /// Normalisierter Text + Einheit; Grundlage der Dublettenerkennung.
    /// </summary>
    [Required]
    [MaxLength(1100)]
    public string Schluessel { get; set; } = string.Empty;

    public DateTime ZuletztVerwendetAm { get; set; }
}
