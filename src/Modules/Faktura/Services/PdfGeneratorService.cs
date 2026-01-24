using Kuestencode.Core.Enums;
using Kuestencode.Core.Models;
using Kuestencode.Faktura.Data;
using Kuestencode.Faktura.Models;
using Kuestencode.Faktura.Services.Pdf;
using Kuestencode.Faktura.Services.Pdf.Layouts;
using Kuestencode.Shared.ApiClients;
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
    private readonly FakturaDbContext _context;
    private readonly IHostApiClient _hostApiClient;
    private readonly IWebHostEnvironment _environment;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PdfGeneratorService> _logger;

    public PdfGeneratorService(
        FakturaDbContext context,
        IHostApiClient hostApiClient,
        IWebHostEnvironment environment,
        IServiceProvider serviceProvider,
        ILogger<PdfGeneratorService> logger)
    {
        _context = context;
        _hostApiClient = hostApiClient;
        _environment = environment;
        _serviceProvider = serviceProvider;
        _logger = logger;

        // QuestPDF License configuration for community use
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateInvoicePdf(int invoiceId)
    {
        _logger.LogInformation("PdfGenerator: start (InvoiceId={InvoiceId})", invoiceId);

        var invoice = _context.Invoices
            .Include(i => i.Items)
            .Include(i => i.DownPayments)
            .FirstOrDefault(i => i.Id == invoiceId);

        if (invoice == null)
        {
            throw new InvalidOperationException("Rechnung nicht gefunden");
        }

        // Lade Customer und Company via Host API
        _logger.LogInformation("PdfGenerator: loading customer (InvoiceId={InvoiceId}, CustomerId={CustomerId})", invoiceId, invoice.CustomerId);
        var customerDto = _hostApiClient.GetCustomerAsync(invoice.CustomerId).Result;
        if (customerDto != null)
        {
            invoice.Customer = new Customer
            {
                Id = customerDto.Id,
                CustomerNumber = customerDto.CustomerNumber,
                Name = customerDto.Name,
                Address = customerDto.Address,
                PostalCode = customerDto.PostalCode,
                City = customerDto.City,
                Country = customerDto.Country,
                Email = customerDto.Email,
                Phone = customerDto.Phone,
                Notes = customerDto.Notes
            };
        }

        _logger.LogInformation("PdfGenerator: loading company (InvoiceId={InvoiceId})", invoiceId);
        var companyDto = _hostApiClient.GetCompanyAsync().Result;
        if (companyDto == null)
        {
            throw new InvalidOperationException("Firmendaten nicht gefunden");
        }

        var company = new Company
        {
            Id = companyDto.Id,
            OwnerFullName = companyDto.OwnerFullName,
            BusinessName = companyDto.BusinessName,
            Address = companyDto.Address,
            PostalCode = companyDto.PostalCode,
            City = companyDto.City,
            Country = companyDto.Country,
            TaxNumber = companyDto.TaxNumber,
            VatId = companyDto.VatId,
            IsKleinunternehmer = companyDto.IsKleinunternehmer,
            BankName = companyDto.BankName,
            BankAccount = companyDto.BankAccount,
            Bic = companyDto.Bic,
            AccountHolder = companyDto.AccountHolder,
            Email = companyDto.Email,
            Phone = companyDto.Phone,
            Website = companyDto.Website,
            LogoData = companyDto.LogoData,
            LogoContentType = companyDto.LogoContentType,
            PdfLayout = Enum.Parse<PdfLayout>(companyDto.PdfLayout),
            PdfPrimaryColor = companyDto.PdfPrimaryColor,
            PdfAccentColor = companyDto.PdfAccentColor,
            PdfHeaderText = companyDto.PdfHeaderText,
            PdfFooterText = companyDto.PdfFooterText,
            PdfPaymentNotice = companyDto.PdfPaymentNotice
        };

        _logger.LogInformation("PdfGenerator: rendering pdf (InvoiceId={InvoiceId})", invoiceId);
        return GeneratePdfWithCompany(invoice, company);
    }

    public byte[] GeneratePdfWithCompany(Invoice invoice, Company company)
    {
        // Select the appropriate layout renderer based on company settings
        var layoutRenderer = GetLayoutRenderer(company.PdfLayout);
        var sw = System.Diagnostics.Stopwatch.StartNew();

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

        var pdfBytes = document.GeneratePdf();
        _logger.LogInformation(
            "PdfGenerator: done (InvoiceId={InvoiceId}, Size={Size}, Ms={Ms})",
            invoice.Id,
            pdfBytes.Length,
            sw.ElapsedMilliseconds);
        return pdfBytes;
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
