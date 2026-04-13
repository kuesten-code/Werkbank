namespace Kuestencode.Shared.Contracts.Recepta;

/// <summary>
/// Leichtgewichtiges Beleg-DTO für die Verwendung durch andere Module (z.B. Acta, Saldo).
/// </summary>
public class ReceptaDocumentDto
{
    public Guid Id { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateOnly InvoiceDate { get; set; }
    public DateOnly? PaidDate { get; set; }
    public decimal AmountNet { get; set; }
    public decimal AmountTax { get; set; }
    public decimal AmountGross { get; set; }
    public decimal TaxRate { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool HasBeenAttached { get; set; }

    /// <summary>
    /// Projektseitiger Anteil (Netto), wenn der Beleg auf mehrere Projekte aufgeteilt wurde.
    /// Null wenn kein Split vorliegt (= voller Betrag gehört diesem Projekt).
    /// </summary>
    public decimal? AllocatedNet { get; set; }

    /// <summary>
    /// Projektseitiger Anteil (Brutto), wenn der Beleg auf mehrere Projekte aufgeteilt wurde.
    /// </summary>
    public decimal? AllocatedGross { get; set; }
}
