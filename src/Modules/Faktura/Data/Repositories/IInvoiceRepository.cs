using Kuestencode.Faktura.Models;

namespace Kuestencode.Faktura.Data.Repositories;

public interface IInvoiceRepository : IRepository<Invoice>
{
    Task<bool> InvoiceNumberExistsAsync(string invoiceNumber);
    Task<string> GenerateInvoiceNumberAsync();
    Task<(string Prefix, string Suffix, int SequenceLength)> GetInvoiceNumberFormatPartsAsync();
    Task<string> GenerateCreditNoteNumberAsync();
    Task<(string Prefix, string Suffix, int SequenceLength)> GetCreditNoteNumberFormatPartsAsync();
    Task<IEnumerable<Invoice>> GetByStatusAsync(InvoiceStatus status);
    Task<IEnumerable<Invoice>> GetByTypeAsync(InvoiceType type);
    Task<IEnumerable<Invoice>> GetPaidByDateRangeAsync(DateTime paidFrom, DateTime paidTo);
    Task<IEnumerable<Invoice>> GetOpenInvoicesByInvoiceDateRangeAsync(DateTime from, DateTime to);
    Task<Invoice?> GetWithDetailsAsync(int id);
    Task<IEnumerable<Invoice>> GetByProjectIdAsync(int projectId);
}
