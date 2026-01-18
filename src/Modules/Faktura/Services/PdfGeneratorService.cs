using Kuestencode.Faktura.Data;
using Kuestencode.Faktura.Models;
using Kuestencode.Faktura.Services.Pdf;
using Kuestencode.Faktura.Services.Pdf.Layouts;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Kuestencode.Faktura.Services;

/// <summary>
/// Coordinates PDF generation by selecting the appropriate layout renderer.
/// This is the main service that implements IPdfGeneratorService.
/// </summary>
public class PdfGeneratorService : IPdfGeneratorService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly IServiceProvider _serviceProvider;

    public PdfGeneratorService(
        ApplicationDbContext context,
        IWebHostEnvironment environment,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _environment = environment;
        _serviceProvider = serviceProvider;

        // QuestPDF License configuration for community use
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateInvoicePdf(int invoiceId)
    {
        var invoice = _context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Items)
            .Include(i => i.DownPayments)
            .FirstOrDefault(i => i.Id == invoiceId);

        if (invoice == null)
        {
            throw new InvalidOperationException("Rechnung nicht gefunden");
        }

        var company = _context.Companies.FirstOrDefault();
        if (company == null)
        {
            throw new InvalidOperationException("Firmendaten nicht gefunden");
        }

        return GeneratePdfWithCompany(invoice, company);
    }

    public byte[] GeneratePdfWithCompany(Invoice invoice, Company company)
    {
        // Select the appropriate layout renderer based on company settings
        var layoutRenderer = GetLayoutRenderer(company.PdfLayout);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor("#1A1A1A"));

                page.Header().Element(c => layoutRenderer.RenderHeader(c, invoice, company));
                page.Content().Element(c => layoutRenderer.RenderContent(c, invoice, company));
                page.Footer().Element(c => layoutRenderer.RenderFooter(c, company));
            });
        });

        return document.GeneratePdf();
    }

    public async Task<string> GenerateAndSaveAsync(int invoiceId)
    {
        var invoice = await _context.Invoices.FindAsync(invoiceId);
        if (invoice == null)
        {
            throw new InvalidOperationException("Rechnung nicht gefunden");
        }

        var pdfBytes = GenerateInvoicePdf(invoiceId);

        var invoicesPath = Path.Combine(_environment.WebRootPath, "invoices");
        Directory.CreateDirectory(invoicesPath);

        var fileName = $"{invoice.InvoiceNumber}.pdf";
        var filePath = Path.Combine(invoicesPath, fileName);

        await File.WriteAllBytesAsync(filePath, pdfBytes);

        return fileName;
    }

    /// <summary>
    /// Factory method to get the appropriate layout renderer based on layout type.
    /// </summary>
    private IPdfLayoutRenderer GetLayoutRenderer(PdfLayout layout)
    {
        return layout switch
        {
            PdfLayout.Klar => _serviceProvider.GetRequiredService<KlarLayoutRenderer>(),
            PdfLayout.Strukturiert => _serviceProvider.GetRequiredService<StrukturiertLayoutRenderer>(),
            PdfLayout.Betont => _serviceProvider.GetRequiredService<BetontLayoutRenderer>(),
            _ => _serviceProvider.GetRequiredService<KlarLayoutRenderer>() // Fallback
        };
    }
}
