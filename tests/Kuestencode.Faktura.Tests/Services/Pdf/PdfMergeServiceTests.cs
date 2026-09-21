using FluentAssertions;
using iText.Kernel.Pdf;
using Kuestencode.Faktura.Models;
using Kuestencode.Faktura.Services.Pdf;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Kuestencode.Faktura.Tests.Services.Pdf;

public class PdfMergeServiceTests
{
    private readonly PdfMergeService _service = new(NullLogger<PdfMergeService>.Instance);

    private static byte[] CreatePdf(int pages)
    {
        using var stream = new MemoryStream();
        using (var doc = new PdfDocument(new PdfWriter(stream)))
        {
            for (var i = 0; i < pages; i++)
            {
                doc.AddNewPage();
            }
        }
        return stream.ToArray();
    }

    private static int PageCount(byte[] pdf)
    {
        using var doc = new PdfDocument(new PdfReader(new MemoryStream(pdf)));
        return doc.GetNumberOfPages();
    }

    private static InvoiceAttachment Attachment(byte[] data, bool frozen = false) => new()
    {
        FileName = "anhang.pdf",
        ContentType = "application/pdf",
        Data = data,
        IsFrozenSnapshot = frozen
    };

    [Fact]
    public void MergeForPrint_OhneAnhaenge_GibtRechnungUnveraendertZurueck()
    {
        var invoice = CreatePdf(1);

        _service.MergeForPrint(invoice, []).Should().BeSameAs(invoice);
    }

    [Fact]
    public void MergeForPrint_HaengtPdfAnhaengeAn()
    {
        var result = _service.MergeForPrint(CreatePdf(1), [Attachment(CreatePdf(2))]);

        PageCount(result).Should().Be(3);
    }

    [Fact]
    public void MergeForPrint_IgnoriertEingefrorenenSnapshot()
    {
        var invoice = CreatePdf(1);

        var result = _service.MergeForPrint(invoice, [Attachment(CreatePdf(1), frozen: true)]);

        result.Should().BeSameAs(invoice);
    }

    [Fact]
    public void MergeForPrint_MitSnapshotUndAnhang_DruckenNurRechnungUndAnhang()
    {
        var result = _service.MergeForPrint(
            CreatePdf(1),
            [Attachment(CreatePdf(1), frozen: true), Attachment(CreatePdf(2))]);

        PageCount(result).Should().Be(3);
    }
}
