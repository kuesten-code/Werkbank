using System.ComponentModel.DataAnnotations;

namespace Kuestencode.Werkbank.Recepta.Domain.Entities;

/// <summary>
/// OCR-Muster für die automatische Erkennung von Rechnungsdaten eines Lieferanten.
/// </summary>
public class SupplierOcrPattern
{
    public Guid Id { get; set; }

    /// <summary>
    /// Referenz zum Lieferanten.
    /// </summary>
    public Guid SupplierId { get; set; }

    /// <summary>
    /// Name des Felds, das erkannt werden soll (z.B. "InvoiceNumber", "InvoiceDate", "AmountGross").
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Textanker/Kontext vor dem zu erkennenden Wert.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Pattern { get; set; } = string.Empty;

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation Properties
    public Supplier Supplier { get; set; } = null!;
}
