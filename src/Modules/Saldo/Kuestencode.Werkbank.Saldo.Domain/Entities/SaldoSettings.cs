using System.ComponentModel.DataAnnotations;

namespace Kuestencode.Werkbank.Saldo.Domain.Entities;

/// <summary>
/// Modulweite Einstellungen für Saldo (EÜR).
/// Es gibt genau einen Datensatz pro Mandant.
/// </summary>
public class SaldoSettings
{
    public Guid Id { get; set; }

    /// <summary>
    /// Kontenrahmen: "SKR03" oder "SKR04".
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string Kontenrahmen { get; set; } = "SKR03";

    /// <summary>
    /// DATEV-Beraternummer (optional).
    /// </summary>
    [MaxLength(20)]
    public string? BeraterNummer { get; set; }

    /// <summary>
    /// DATEV-Mandantennummer (optional).
    /// </summary>
    [MaxLength(20)]
    public string? MandantenNummer { get; set; }

    /// <summary>
    /// Erster Monat des Wirtschaftsjahres (1–12, default 1 = Januar).
    /// </summary>
    public int WirtschaftsjahrBeginn { get; set; } = 1;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
