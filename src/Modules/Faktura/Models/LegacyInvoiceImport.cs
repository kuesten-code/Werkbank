namespace Kuestencode.Faktura.Models;

public record LegacyInvoiceImport(
    string InvoiceNumber,
    int CustomerId,
    DateTime InvoiceDate,
    DateTime? DueDate,
    decimal NetAmount,
    decimal VatRate,
    decimal AlreadyPaid,
    DateTime? AlreadyPaidDate,
    string OriginalPdfFileName,
    byte[] OriginalPdf,
    string? Notes)
{
    public const int MaxInvoiceNumberLength = 20;

    public static readonly IReadOnlyList<decimal> AllowedVatRates = [19m, 7m, 0m];

    public InvoiceItem ToSummaryItem() => new()
    {
        Position = 1,
        Description = $"Offener Posten aus Vorsystem, Rechnung {InvoiceNumber.Trim()}",
        Quantity = 1,
        UnitPrice = NetAmount,
        VatRate = VatRate
    };

    public decimal GrossAmount => ToSummaryItem().TotalGross;
}
