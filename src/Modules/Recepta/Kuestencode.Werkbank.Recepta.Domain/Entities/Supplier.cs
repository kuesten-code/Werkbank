using System.ComponentModel.DataAnnotations;

namespace Kuestencode.Werkbank.Recepta.Domain.Entities;

/// <summary>
/// Ein Lieferant in der Eingangsrechnungsverwaltung.
/// </summary>
public class Supplier
{
    public Guid Id { get; set; }

    /// <summary>
    /// Eindeutige Lieferantennummer.
    /// </summary>
    [Required(ErrorMessage = "Lieferantennummer ist erforderlich")]
    [MaxLength(50)]
    public string SupplierNumber { get; set; } = string.Empty;

    /// <summary>
    /// Name des Lieferanten.
    /// </summary>
    [Required(ErrorMessage = "Lieferantenname ist erforderlich")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optionale Adresse - Straße.
    /// </summary>
    [MaxLength(200)]
    public string? Address { get; set; }

    /// <summary>
    /// Optionale Postleitzahl.
    /// </summary>
    [MaxLength(10)]
    public string? PostalCode { get; set; }

    /// <summary>
    /// Optionale Stadt.
    /// </summary>
    [MaxLength(100)]
    public string? City { get; set; }

    /// <summary>
    /// Land (ISO-Code), Standard: DE.
    /// </summary>
    [MaxLength(5)]
    public string Country { get; set; } = "DE";

    /// <summary>
    /// Optionale E-Mail-Adresse.
    /// </summary>
    [MaxLength(200)]
    public string? Email { get; set; }

    /// <summary>
    /// Optionale Telefonnummer.
    /// </summary>
    [MaxLength(50)]
    public string? Phone { get; set; }

    /// <summary>
    /// Optionale Umsatzsteuer-Identifikationsnummer.
    /// </summary>
    [MaxLength(50)]
    public string? TaxId { get; set; }

    /// <summary>
    /// Optionale IBAN.
    /// </summary>
    [MaxLength(34)]
    public string? Iban { get; set; }

    /// <summary>
    /// Optionale BIC.
    /// </summary>
    [MaxLength(11)]
    public string? Bic { get; set; }

    /// <summary>
    /// Optionale Notizen.
    /// </summary>
    [MaxLength(2000)]
    public string? Notes { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation Properties
    public List<Document> Documents { get; set; } = new();
    public List<SupplierOcrPattern> OcrPatterns { get; set; } = new();
}
