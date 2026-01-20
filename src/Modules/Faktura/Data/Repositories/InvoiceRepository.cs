using Kuestencode.Faktura.Models;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Faktura.Data.Repositories;

public class InvoiceRepository : Repository<Invoice>, IInvoiceRepository
{
    public InvoiceRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber)
    {
        return await _dbSet
            .Include(i => i.Customer)
            .Include(i => i.Items)
            .Include(i => i.DownPayments)
            .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber);
    }

    public async Task<bool> InvoiceNumberExistsAsync(string invoiceNumber)
    {
        return await _dbSet
            .AnyAsync(i => i.InvoiceNumber == invoiceNumber);
    }

    public async Task<string> GenerateInvoiceNumberAsync()
    {
        // Hole das Rechnungsprefix aus den Firmenstammdaten
        var company = await _context.Companies.FirstOrDefaultAsync();
        var prefix = !string.IsNullOrWhiteSpace(company?.InvoiceNumberPrefix)
            ? company.InvoiceNumberPrefix.Trim()
            : string.Empty;

        var currentYear = DateTime.Now.Year;
        var yearPrefix = currentYear.ToString();

        // Kombiniere Prefix, Jahr und Nummer (z.B. "KC-2025-0001" oder "2025-0001")
        var searchPrefix = !string.IsNullOrEmpty(prefix)
            ? $"{prefix}-{yearPrefix}"
            : yearPrefix;

        var lastInvoice = await _dbSet
            .Where(i => i.InvoiceNumber.StartsWith(searchPrefix))
            .OrderByDescending(i => i.InvoiceNumber)
            .FirstOrDefaultAsync();

        if (lastInvoice == null)
        {
            return !string.IsNullOrEmpty(prefix)
                ? $"{prefix}-{yearPrefix}-0001"
                : $"{yearPrefix}-0001";
        }

        // Extrahiere Nummer aus letzter Rechnungsnummer
        // Format: "PREFIX-2025-0001" oder "2025-0001"
        var parts = lastInvoice.InvoiceNumber.Split('-');
        var numberPart = parts[^1]; // Letzter Teil ist immer die Nummer

        if (int.TryParse(numberPart, out int lastNumber))
        {
            var nextNumber = lastNumber + 1;
            return !string.IsNullOrEmpty(prefix)
                ? $"{prefix}-{yearPrefix}-{nextNumber:D4}"
                : $"{yearPrefix}-{nextNumber:D4}";
        }

        // Fallback wenn Parsing fehlschlägt
        return !string.IsNullOrEmpty(prefix)
            ? $"{prefix}-{yearPrefix}-0001"
            : $"{yearPrefix}-0001";
    }

    public async Task<IEnumerable<Invoice>> GetByCustomerIdAsync(int customerId)
    {
        return await _dbSet
            .Include(i => i.Customer)
            .Include(i => i.Items)
            .Include(i => i.DownPayments)
            .Where(i => i.CustomerId == customerId)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Invoice>> GetByStatusAsync(InvoiceStatus status)
    {
        return await _dbSet
            .Include(i => i.Customer)
            .Include(i => i.Items)
            .Include(i => i.DownPayments)
            .Where(i => i.Status == status)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Invoice>> GetOverdueInvoicesAsync()
    {
        var today = DateTime.Today;

        return await _dbSet
            .Include(i => i.Customer)
            .Include(i => i.Items)
            .Include(i => i.DownPayments)
            .Where(i => i.Status == InvoiceStatus.Sent &&
                       i.DueDate.HasValue &&
                       i.DueDate.Value < today)
            .OrderBy(i => i.DueDate)
            .ToListAsync();
    }

    public async Task<Invoice?> GetWithDetailsAsync(int id)
    {
        return await _dbSet
            .Include(i => i.Customer)
            .Include(i => i.Items.OrderBy(item => item.Position))
            .Include(i => i.DownPayments)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public override async Task<IEnumerable<Invoice>> GetAllAsync()
    {
        return await _dbSet
            .Include(i => i.Customer)
            .Include(i => i.Items)
            .Include(i => i.DownPayments)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();
    }

    public override async Task<Invoice?> GetByIdAsync(int id)
    {
        return await GetWithDetailsAsync(id);
    }
}
