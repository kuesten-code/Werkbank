using Kuestencode.Faktura.Models;

namespace Kuestencode.Faktura.Data.Repositories;

public interface IInvoiceRepository : IRepository<Invoice>
{
    Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber);
    Task<bool> InvoiceNumberExistsAsync(string invoiceNumber);
    Task<string> GenerateInvoiceNumberAsync();
    Task<IEnumerable<Invoice>> GetByCustomerIdAsync(int customerId);
    Task<IEnumerable<Invoice>> GetByStatusAsync(InvoiceStatus status);
    Task<IEnumerable<Invoice>> GetOverdueInvoicesAsync();
    Task<Invoice?> GetWithDetailsAsync(int id);
}
