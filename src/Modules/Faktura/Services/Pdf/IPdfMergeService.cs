using Kuestencode.Faktura.Models;

namespace Kuestencode.Faktura.Services.Pdf;

public interface IPdfMergeService
{
    byte[] MergeForPrint(byte[] invoicePdf, IEnumerable<InvoiceAttachment> attachments);
}
