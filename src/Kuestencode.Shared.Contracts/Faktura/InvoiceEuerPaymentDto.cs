namespace Kuestencode.Shared.Contracts.Faktura;

/// <summary>
/// Einzelne Zahlung einer Faktura-Rechnung, angereichert mit Rechnungspositionen für die EÜR.
/// Pro Rechnung kann es mehrere Einträge geben (Teilzahlungen).
/// </summary>
public class InvoiceEuerPaymentDto
{
    public int PaymentId { get; set; }
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string InvoiceType { get; set; } = "Invoice";
    public DateTime InvoiceDate { get; set; }
    public DateOnly PaymentDate { get; set; }
    public decimal PaymentAmount { get; set; }
    public decimal InvoiceTotalGross { get; set; }
    public string? CustomerName { get; set; }
    public List<InvoiceItemDto> Items { get; set; } = [];
}
