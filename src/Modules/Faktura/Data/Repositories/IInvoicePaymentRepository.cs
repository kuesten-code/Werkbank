using Kuestencode.Faktura.Models;

namespace Kuestencode.Faktura.Data.Repositories;

public interface IInvoicePaymentRepository : IRepository<InvoicePayment>
{
    Task<IEnumerable<InvoicePayment>> GetByInvoiceIdAsync(int invoiceId);
    Task<IEnumerable<InvoicePayment>> GetByPaymentDateRangeAsync(DateTime von, DateTime bis);
}
