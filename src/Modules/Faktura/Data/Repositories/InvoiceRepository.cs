using Kuestencode.Faktura.Models;
using Kuestencode.Shared.ApiClients;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Faktura.Data.Repositories;

public class InvoiceRepository : Repository<Invoice>, IInvoiceRepository
{
    private readonly IHostApiClient _hostApiClient;

    public InvoiceRepository(
        FakturaDbContext context,
        IHostApiClient hostApiClient) : base(context)
    {
        _hostApiClient = hostApiClient;
    }

    public async Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber)
    {
        var invoice = await _dbSet
            .Include(i => i.Items)
            .Include(i => i.DownPayments)
            .Include(i => i.Attachments)
            .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber);

        if (invoice != null)
        {
            await LoadCustomerAsync(invoice);
        }

        return invoice;
    }

    public async Task<bool> InvoiceNumberExistsAsync(string invoiceNumber)
    {
        return await _dbSet
            .AnyAsync(i => i.InvoiceNumber == invoiceNumber);
    }

    public async Task<string> GenerateInvoiceNumberAsync()
    {
        // Hole das Rechnungsprefix aus den Firmenstammdaten via Host API
        var company = await _hostApiClient.GetCompanyAsync();
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
        var invoices = await _dbSet
            .Include(i => i.Items)
            .Include(i => i.DownPayments)
            .Include(i => i.Attachments)
            .Where(i => i.CustomerId == customerId)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();

        await LoadCustomersAsync(invoices);
        return invoices;
    }

    public async Task<IEnumerable<Invoice>> GetByStatusAsync(InvoiceStatus status)
    {
        var invoices = await _dbSet
            .Include(i => i.Items)
            .Include(i => i.DownPayments)
            .Include(i => i.Attachments)
            .Where(i => i.Status == status)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();

        await LoadCustomersAsync(invoices);
        return invoices;
    }

    public async Task<IEnumerable<Invoice>> GetOverdueInvoicesAsync()
    {
        var today = DateTime.Today;

        var invoices = await _dbSet
            .Include(i => i.Items)
            .Include(i => i.DownPayments)
            .Include(i => i.Attachments)
            .Where(i => i.Status == InvoiceStatus.Sent &&
                       i.DueDate.HasValue &&
                       i.DueDate.Value < today)
            .OrderBy(i => i.DueDate)
            .ToListAsync();

        await LoadCustomersAsync(invoices);
        return invoices;
    }

    public async Task<Invoice?> GetWithDetailsAsync(int id)
    {
        var invoice = await _dbSet
            .Include(i => i.Items.OrderBy(item => item.Position))
            .Include(i => i.DownPayments)
            .Include(i => i.Attachments)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice != null)
        {
            await LoadCustomerAsync(invoice);
        }

        return invoice;
    }

    public override async Task<IEnumerable<Invoice>> GetAllAsync()
    {
        var invoices = await _dbSet
            .Include(i => i.Items)
            .Include(i => i.DownPayments)
            .Include(i => i.Attachments)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();

        await LoadCustomersAsync(invoices);
        return invoices;
    }

    public override async Task<Invoice?> GetByIdAsync(int id)
    {
        return await GetWithDetailsAsync(id);
    }

    /// <summary>
    /// Lädt den Kunden für eine einzelne Rechnung via Host API.
    /// </summary>
    private async Task LoadCustomerAsync(Invoice invoice)
    {
        var customerDto = await _hostApiClient.GetCustomerAsync(invoice.CustomerId);
        if (customerDto != null)
        {
            invoice.Customer = new Core.Models.Customer
            {
                Id = customerDto.Id,
                CustomerNumber = customerDto.CustomerNumber,
                Name = customerDto.Name,
                Address = customerDto.Address,
                PostalCode = customerDto.PostalCode,
                City = customerDto.City,
                Country = customerDto.Country,
                Email = customerDto.Email,
                Phone = customerDto.Phone,
                Notes = customerDto.Notes,
                Salutation = customerDto.Salutation
            };
        }
    }

    /// <summary>
    /// Lädt Kunden für mehrere Rechnungen effizient via Host API.
    /// </summary>
    private async Task LoadCustomersAsync(IEnumerable<Invoice> invoices)
    {
        var customerIds = invoices.Select(i => i.CustomerId).Distinct().ToList();

        // Alle Kunden via Host API laden
        var allCustomerDtos = await _hostApiClient.GetAllCustomersAsync();
        var customerDict = allCustomerDtos
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionary(c => c.Id, c => new Core.Models.Customer
            {
                Id = c.Id,
                CustomerNumber = c.CustomerNumber,
                Name = c.Name,
                Address = c.Address,
                PostalCode = c.PostalCode,
                City = c.City,
                Country = c.Country,
                Email = c.Email,
                Phone = c.Phone,
                Notes = c.Notes,
                Salutation = c.Salutation
            });

        foreach (var invoice in invoices)
        {
            if (customerDict.TryGetValue(invoice.CustomerId, out var customer))
            {
                invoice.Customer = customer;
            }
        }
    }
}
