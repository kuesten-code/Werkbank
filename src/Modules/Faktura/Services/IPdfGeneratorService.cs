using Kuestencode.Core.Models;
using Kuestencode.Faktura.Models;

namespace Kuestencode.Faktura.Services;

public interface IPdfGeneratorService
{
    Task<byte[]> GenerateInvoicePdfAsync(int invoiceId);
    byte[] GenerateInvoicePdf(int invoiceId);
    byte[] GeneratePdfWithCompany(Invoice invoice, Company company);
    Task<string> GenerateAndSaveAsync(int invoiceId);
}
