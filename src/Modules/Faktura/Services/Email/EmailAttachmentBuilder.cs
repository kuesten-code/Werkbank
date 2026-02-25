using Kuestencode.Faktura.Data;
using Kuestencode.Faktura.Models;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using System.Diagnostics;

namespace Kuestencode.Faktura.Services.Email;

/// <summary>
/// Builds email attachments for different invoice formats
/// </summary>
public class EmailAttachmentBuilder : IEmailAttachmentBuilder
{
    private readonly IPdfGeneratorService _pdfGenerator;
    private readonly IXRechnungService _xRechnungService;
    private readonly FakturaDbContext _dbContext;
    private readonly ILogger<EmailAttachmentBuilder> _logger;

    public EmailAttachmentBuilder(
        IPdfGeneratorService pdfGenerator,
        IXRechnungService xRechnungService,
        FakturaDbContext dbContext,
        ILogger<EmailAttachmentBuilder> logger)
    {
        _pdfGenerator = pdfGenerator;
        _xRechnungService = xRechnungService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task AddInvoiceAttachmentsAsync(
        BodyBuilder bodyBuilder,
        int invoiceId,
        string invoiceNumber,
        EmailAttachmentFormat format)
    {
        _logger.LogInformation(
            "EmailAttachmentBuilder: start (InvoiceId={InvoiceId}, Format={Format})",
            invoiceId,
            format);

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

        _logger.LogInformation("EmailAttachmentBuilder: base attachments done (InvoiceId={InvoiceId})", invoiceId);
        await AddCustomAttachmentsAsync(bodyBuilder, invoiceId);
        _logger.LogInformation("EmailAttachmentBuilder: custom attachments done (InvoiceId={InvoiceId})", invoiceId);
    }

    private async Task AddNormalPdfAsync(BodyBuilder bodyBuilder, int invoiceId, string invoiceNumber)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("EmailAttachmentBuilder: generating PDF (InvoiceId={InvoiceId})", invoiceId);
        var pdfBytes = await _pdfGenerator.GenerateInvoicePdfAsync(invoiceId);
        bodyBuilder.Attachments.Add(
            $"Rechnung_{invoiceNumber}.pdf",
            pdfBytes,
            new ContentType("application", "pdf"));
        _logger.LogInformation(
            "EmailAttachmentBuilder: PDF ready (InvoiceId={InvoiceId}, Size={Size}, Ms={Ms})",
            invoiceId,
            pdfBytes.Length,
            sw.ElapsedMilliseconds);
    }

    private async Task AddZugferdPdfAsync(BodyBuilder bodyBuilder, int invoiceId, string invoiceNumber)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("EmailAttachmentBuilder: generating ZUGFeRD PDF (InvoiceId={InvoiceId})", invoiceId);
        var zugferdPdf = await _xRechnungService.GenerateZugferdPdfAsync(invoiceId);
        bodyBuilder.Attachments.Add(
            $"Rechnung_{invoiceNumber}_zugferd.pdf",
            zugferdPdf,
            new ContentType("application", "pdf"));
        _logger.LogInformation(
            "EmailAttachmentBuilder: ZUGFeRD PDF ready (InvoiceId={InvoiceId}, Size={Size}, Ms={Ms})",
            invoiceId,
            zugferdPdf.Length,
            sw.ElapsedMilliseconds);
    }

    private async Task AddXRechnungXmlAsync(BodyBuilder bodyBuilder, int invoiceId, string invoiceNumber)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("EmailAttachmentBuilder: generating XRechnung XML (InvoiceId={InvoiceId})", invoiceId);
        var xmlContent = await _xRechnungService.GenerateXRechnungXmlAsync(invoiceId);
        var xmlBytes = System.Text.Encoding.UTF8.GetBytes(xmlContent);
        bodyBuilder.Attachments.Add(
            $"Rechnung_{invoiceNumber}_xrechnung.xml",
            xmlBytes,
            new ContentType("application", "xml"));
        _logger.LogInformation(
            "EmailAttachmentBuilder: XRechnung XML ready (InvoiceId={InvoiceId}, Size={Size}, Ms={Ms})",
            invoiceId,
            xmlBytes.Length,
            sw.ElapsedMilliseconds);
    }

    private async Task AddCustomAttachmentsAsync(BodyBuilder bodyBuilder, int invoiceId)
    {
        _logger.LogInformation("EmailAttachmentBuilder: loading custom attachments (InvoiceId={InvoiceId})", invoiceId);
        var attachments = await _dbContext.InvoiceAttachments
            .AsNoTracking()
            .Where(a => a.InvoiceId == invoiceId)
            .ToListAsync();

        foreach (var attachment in attachments)
        {
            var contentType = ParseContentType(attachment.ContentType);
            bodyBuilder.Attachments.Add(
                attachment.FileName,
                attachment.Data,
                contentType);
        }
    }

    private static ContentType ParseContentType(string? contentType)
    {
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            try
            {
                return ContentType.Parse(contentType);
            }
            catch
            {
                // Fallback below.
            }
        }

        return new ContentType("application", "octet-stream");
    }
}
