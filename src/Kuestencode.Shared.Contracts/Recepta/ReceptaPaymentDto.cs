namespace Kuestencode.Shared.Contracts.Recepta;

/// <summary>
/// Einzelne Zahlung eines Recepta-Belegs für die EÜR.
/// Pro Beleg kann es mehrere Einträge geben (Teilzahlungen).
/// </summary>
public class ReceptaPaymentDto
{
    public Guid PaymentId { get; set; }
    public Guid DocumentId { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public DateOnly InvoiceDate { get; set; }
    public DateOnly PaymentDate { get; set; }
    public decimal PaymentAmount { get; set; }
    public decimal AmountNet { get; set; }
    public decimal AmountTax { get; set; }
    public decimal AmountGross { get; set; }
    public decimal TaxRate { get; set; }
    public string Category { get; set; } = string.Empty;
}
