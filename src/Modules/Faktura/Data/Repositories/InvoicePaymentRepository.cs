using Kuestencode.Faktura.Models;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Faktura.Data.Repositories;

public class InvoicePaymentRepository : Repository<InvoicePayment>, IInvoicePaymentRepository
{
    public InvoicePaymentRepository(FakturaDbContext context) : base(context) { }

    public async Task<IEnumerable<InvoicePayment>> GetByInvoiceIdAsync(int invoiceId)
    {
        return await _dbSet
            .Where(p => p.InvoiceId == invoiceId)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<InvoicePayment>> GetByPaymentDateRangeAsync(DateTime von, DateTime bis)
    {
        return await _dbSet
            .Where(p => p.PaymentDate >= von && p.PaymentDate <= bis)
            .Include(p => p.Invoice)
                .ThenInclude(i => i.Items)
            .OrderBy(p => p.PaymentDate)
            .ToListAsync();
    }
}
