namespace Kuestencode.Shared.Contracts.Recepta;

/// <summary>
/// Leichtgewichtiges Beleg-DTO für die Verwendung durch andere Module (z.B. Acta).
/// </summary>
public class ReceptaDocumentDto
{
    public Guid Id { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateOnly InvoiceDate { get; set; }
    public decimal AmountNet { get; set; }
    public decimal AmountGross { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
