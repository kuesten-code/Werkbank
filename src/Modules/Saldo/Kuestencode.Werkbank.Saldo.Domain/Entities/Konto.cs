using System.ComponentModel.DataAnnotations;
using Kuestencode.Werkbank.Saldo.Domain.Enums;

namespace Kuestencode.Werkbank.Saldo.Domain.Entities;

/// <summary>
/// Eintrag im Kontenstamm (SKR03 oder SKR04).
/// </summary>
public class Konto
{
    public Guid Id { get; set; }

    /// <summary>
    /// Kontenrahmen: "SKR03" oder "SKR04".
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string Kontenrahmen { get; set; } = "SKR03";

    /// <summary>
    /// Kontonummer (z.B. "8400").
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string KontoNummer { get; set; } = string.Empty;

    /// <summary>
    /// Kontobezeichnung (z.B. "Erlöse 19% USt").
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string KontoBezeichnung { get; set; } = string.Empty;

    /// <summary>
    /// Art des Kontos (Einnahme, Ausgabe, Bank, ...).
    /// </summary>
    public KontoTyp KontoTyp { get; set; }

    /// <summary>
    /// Standardmäßiger Umsatzsteuersatz (19, 7, 0 oder null wenn nicht relevant).
    /// </summary>
    public decimal? UstSatz { get; set; }

    /// <summary>
    /// Gibt an, ob das Konto aktiv ist.
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation
    public List<KategorieKontoMapping> Mappings { get; set; } = new();
}
