using System.ComponentModel.DataAnnotations;
using Kuestencode.Werkbank.Recepta.Domain.Enums;

namespace Kuestencode.Werkbank.Saldo.Domain.Entities;

/// <summary>
/// Zuordnung einer Recepta-Kategorie (DocumentCategory) zu einem Konto.
/// </summary>
public class KategorieKontoMapping
{
    public Guid Id { get; set; }

    /// <summary>
    /// Kontenrahmen: "SKR03" oder "SKR04".
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string Kontenrahmen { get; set; } = "SKR03";

    /// <summary>
    /// Recepta-Kategorie aus dem Belegmodul.
    /// Gespeichert als String (Enum-Name) analog zu Recepta.
    /// </summary>
    [Required]
    [MaxLength(30)]
    public string ReceiptaKategorie { get; set; } = string.Empty;

    /// <summary>
    /// Ziel-Kontonummer in dem gewählten Kontenrahmen.
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string KontoNummer { get; set; } = string.Empty;

    /// <summary>
    /// True wenn der Nutzer das Mapping manuell angepasst hat.
    /// False = Standard-Mapping.
    /// </summary>
    public bool IsCustom { get; set; } = false;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public Konto? Konto { get; set; }
}
