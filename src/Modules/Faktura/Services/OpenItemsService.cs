using Kuestencode.Faktura.Data.Repositories;
using Kuestencode.Faktura.Models;

namespace Kuestencode.Faktura.Services;

public class OpenItemsService : IOpenItemsService
{
    private readonly IInvoiceRepository _invoiceRepository;

    public OpenItemsService(IInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
    }

    public async Task<IReadOnlyList<OpenItem>> GetOpenItemsAsync(DateTime from, DateTime to)
    {
        var invoices = await _invoiceRepository.GetOpenInvoicesByInvoiceDateRangeAsync(from, to);

        return invoices
            .Select(i => new OpenItem(
                i.InvoiceNumber,
                i.InvoiceDate,
                i.Customer?.Name ?? string.Empty,
                i.EffectiveDueDate,
                i.TotalGross,
                i.TotalPaid,
                i.RemainingAmount))
            .ToList();
    }
}
