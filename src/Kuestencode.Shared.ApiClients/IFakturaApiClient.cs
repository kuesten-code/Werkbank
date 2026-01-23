using Kuestencode.Shared.Contracts.Faktura;

namespace Kuestencode.Shared.ApiClients;

public interface IFakturaApiClient
{
    Task<List<InvoiceDto>> GetAllInvoicesAsync(InvoiceFilterDto? filter = null);
    Task<InvoiceDto?> GetInvoiceAsync(int id);
    Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceRequest request);
    Task UpdateInvoiceAsync(int id, UpdateInvoiceRequest request);
    Task DeleteInvoiceAsync(int id);
    Task SendInvoiceAsync(int id);
    Task<byte[]> GenerateInvoicePdfAsync(int id);
    Task MarkAsPaidAsync(int id, DateTime paidDate);
}
