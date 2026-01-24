using System.IO;
using iText.Kernel.Pdf;
using iText.Kernel.Utils;
using Kuestencode.Faktura.Models;

namespace Kuestencode.Faktura.Services.Pdf;

public class PdfMergeService : IPdfMergeService
{
    private readonly ILogger<PdfMergeService> _logger;

    public PdfMergeService(ILogger<PdfMergeService> logger)
    {
        _logger = logger;
    }

    public byte[] MergeForPrint(byte[] invoicePdf, IEnumerable<InvoiceAttachment> attachments)
    {
        var pdfAttachments = attachments
            .Where(a =>
                (a.ContentType?.Contains("pdf", StringComparison.OrdinalIgnoreCase) ?? false) ||
                a.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            .Where(a => a.Data != null && a.Data.Length > 0)
            .ToList();

        if (pdfAttachments.Count == 0)
        {
            return invoicePdf;
        }

        try
        {
            using var outputStream = new MemoryStream();
            using var writer = new PdfWriter(outputStream);
            using var destPdf = new PdfDocument(writer);
            var merger = new PdfMerger(destPdf);

            using (var invoiceStream = new MemoryStream(invoicePdf))
            using (var invoiceReader = new PdfReader(invoiceStream))
            using (var invoiceDoc = new PdfDocument(invoiceReader))
            {
                merger.Merge(invoiceDoc, 1, invoiceDoc.GetNumberOfPages());
            }

            foreach (var attachment in pdfAttachments)
            {
                using var attachmentStream = new MemoryStream(attachment.Data);
                using var attachmentReader = new PdfReader(attachmentStream);
                using var attachmentDoc = new PdfDocument(attachmentReader);
                merger.Merge(attachmentDoc, 1, attachmentDoc.GetNumberOfPages());
            }

            destPdf.Close();
            return outputStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PdfMergeService: merge failed, falling back to invoice only.");
            return invoicePdf;
        }
    }
}
