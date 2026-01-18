using Kuestencode.Faktura.Models;
using MimeKit;

namespace Kuestencode.Faktura.Services.Email;

/// <summary>
/// Builds email attachments for different invoice formats
/// </summary>
public class EmailAttachmentBuilder : IEmailAttachmentBuilder
{
    private readonly IPdfGeneratorService _pdfGenerator;
    private readonly IXRechnungService _xRechnungService;

    public EmailAttachmentBuilder(
        IPdfGeneratorService pdfGenerator,
        IXRechnungService xRechnungService)
    {
        _pdfGenerator = pdfGenerator;
        _xRechnungService = xRechnungService;
    }

    public async Task AddInvoiceAttachmentsAsync(
        BodyBuilder bodyBuilder,
        int invoiceId,
        string invoiceNumber,
        EmailAttachmentFormat format)
    {
        switch (format)
        {
            case EmailAttachmentFormat.NormalPdf:
                await AddNormalPdfAsync(bodyBuilder, invoiceId, invoiceNumber);
                break;

            case EmailAttachmentFormat.ZugferdPdf:
                await AddZugferdPdfAsync(bodyBuilder, invoiceId, invoiceNumber);
                break;

            case EmailAttachmentFormat.XRechnungXmlOnly:
                await AddXRechnungXmlAsync(bodyBuilder, invoiceId, invoiceNumber);
                break;

            case EmailAttachmentFormat.XRechnungXmlAndPdf:
                await AddXRechnungXmlAsync(bodyBuilder, invoiceId, invoiceNumber);
                await AddNormalPdfAsync(bodyBuilder, invoiceId, invoiceNumber);
                break;

            default:
                throw new ArgumentException($"Unsupported format: {format}");
        }
    }

    private Task AddNormalPdfAsync(BodyBuilder bodyBuilder, int invoiceId, string invoiceNumber)
    {
        var pdfBytes = _pdfGenerator.GenerateInvoicePdf(invoiceId);
        bodyBuilder.Attachments.Add(
            $"Rechnung_{invoiceNumber}.pdf",
            pdfBytes,
            new ContentType("application", "pdf"));
        return Task.CompletedTask;
    }

    private async Task AddZugferdPdfAsync(BodyBuilder bodyBuilder, int invoiceId, string invoiceNumber)
    {
        var zugferdPdf = await _xRechnungService.GenerateZugferdPdfAsync(invoiceId);
        bodyBuilder.Attachments.Add(
            $"Rechnung_{invoiceNumber}_zugferd.pdf",
            zugferdPdf,
            new ContentType("application", "pdf"));
    }

    private async Task AddXRechnungXmlAsync(BodyBuilder bodyBuilder, int invoiceId, string invoiceNumber)
    {
        var xmlContent = await _xRechnungService.GenerateXRechnungXmlAsync(invoiceId);
        var xmlBytes = System.Text.Encoding.UTF8.GetBytes(xmlContent);
        bodyBuilder.Attachments.Add(
            $"Rechnung_{invoiceNumber}_xrechnung.xml",
            xmlBytes,
            new ContentType("application", "xml"));
    }
}
