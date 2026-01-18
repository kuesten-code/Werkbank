using Kuestencode.Faktura.Models;

namespace Kuestencode.Faktura.Services;

public interface IPdfGeneratorService
{
    byte[] GenerateInvoicePdf(int invoiceId);
    byte[] GeneratePdfWithCompany(Invoice invoice, Company company);
    Task<string> GenerateAndSaveAsync(int invoiceId);
}
