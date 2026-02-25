using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Kuestencode.Werkbank.Recepta.Domain.Enums;

namespace Kuestencode.Werkbank.Recepta.Domain.Entities;

/// <summary>
/// Ein Beleg (Eingangsrechnung) in der Eingangsrechnungsverwaltung.
/// </summary>
public class Document
{
    public Guid Id { get; set; }

    /// <summary>
    /// Interne Belegnummer.
    /// </summary>
    [Required(ErrorMessage = "Belegnummer ist erforderlich")]
    [MaxLength(50)]
    public string DocumentNumber { get; set; } = string.Empty;

    /// <summary>
    /// Referenz zum Lieferanten.
    /// </summary>
    public Guid SupplierId { get; set; }

    /// <summary>
    /// Rechnungsnummer des Lieferanten.
    /// </summary>
    [Required(ErrorMessage = "Rechnungsnummer ist erforderlich")]
    [MaxLength(100)]
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Rechnungsdatum.
    /// </summary>
    public DateOnly InvoiceDate { get; set; }

    /// <summary>
    /// Optionales Fälligkeitsdatum.
    /// </summary>
    public DateOnly? DueDate { get; set; }

    /// <summary>
    /// Nettobetrag.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal AmountNet { get; set; }

    /// <summary>
    /// Steuersatz in Prozent (z.B. 19).
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal TaxRate { get; set; }

    /// <summary>
    /// Steuerbetrag.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal AmountTax { get; set; }

    /// <summary>
    /// Bruttobetrag.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal AmountGross { get; set; }

    /// <summary>
    /// Kategorie des Belegs.
    /// </summary>
    public DocumentCategory Category { get; set; } = DocumentCategory.Other;

    /// <summary>
    /// Status des Belegs.
    /// </summary>
    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;

    /// <summary>
    /// Optionale Referenz auf ein Acta-Projekt.
    /// </summary>
    public Guid? ProjectId { get; set; }

    /// <summary>
    /// Gibt an, ob der Beleg bereits als Rechnungsanhang in Faktura verwendet wurde.
    /// </summary>
    public bool HasBeenAttached { get; set; }

    /// <summary>
    /// OCR-Rohtext aus dem gescannten Dokument.
    /// </summary>
    public string? OcrRawText { get; set; }

    /// <summary>
    /// Optionale Notizen.
    /// </summary>
    [MaxLength(2000)]
    public string? Notes { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation Properties
    public Supplier Supplier { get; set; } = null!;
    public List<DocumentFile> Files { get; set; } = new();

    // Berechnete Eigenschaften

    /// <summary>
    /// Prüft, ob der Beleg bearbeitet werden kann.
    /// </summary>
    [NotMapped]
    public bool IsEditable => Status == DocumentStatus.Draft;

    /// <summary>
    /// Prüft, ob der Beleg überfällig ist.
    /// </summary>
    [NotMapped]
    public bool IsOverdue => DueDate.HasValue
        && DateOnly.FromDateTime(DateTime.UtcNow) > DueDate.Value
        && Status != DocumentStatus.Paid;
}
