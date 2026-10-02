namespace Kuestencode.Faktura.Models;

public record OpenItem(
    string InvoiceNumber,
    DateTime InvoiceDate,
    string CustomerName,
    DateTime? DueDate,
    decimal TotalGross,
    decimal TotalPaid,
    decimal OpenAmount);
