namespace Kuestencode.Shared.Contracts.Faktura;

public class ProjectInvoicesResponseDto
{
    public int ProjectId { get; set; }
    public decimal TotalNet { get; set; }
    public decimal TotalGross { get; set; }
    public int InvoiceCount { get; set; }
    public List<InvoiceDto> Invoices { get; set; } = new();
}
