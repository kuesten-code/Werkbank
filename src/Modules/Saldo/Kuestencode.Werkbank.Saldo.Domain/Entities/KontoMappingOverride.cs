using System.ComponentModel.DataAnnotations;

namespace Kuestencode.Werkbank.Saldo.Domain.Entities;

/// <summary>
/// Benutzerdefinierte Überschreibung eines Kategorie-Konto-Mappings.
/// Hat Vorrang vor dem Standard-Mapping aus KategorieKontoMapping.
/// Pro Kontenrahmen + Kategorie kann genau ein Override existieren.
/// </summary>
public class KontoMappingOverride
{
    public Guid Id { get; set; }

    /// <summary>
    /// Kontenrahmen: "SKR03" oder "SKR04".
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string Kontenrahmen { get; set; } = string.Empty;

    /// <summary>
    /// Recepta-Kategorie als String (Enum-Name, z.B. "Material").
    /// </summary>
    [Required]
    [MaxLength(30)]
    public string Kategorie { get; set; } = string.Empty;

    /// <summary>
    /// Ziel-Kontonummer (überschreibt das Standard-Mapping).
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string KontoNummer { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
