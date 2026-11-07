using FluentAssertions;
using Kuestencode.Core.Models;
using Kuestencode.Faktura.Data;
using Kuestencode.Faktura.Models;
using Kuestencode.Faktura.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kuestencode.Faktura.Tests.Services.Pdf;

public class PdfGeneratorServiceTests : IClassFixture<FakturaWebApplicationFactory>
{
    private readonly FakturaWebApplicationFactory _factory;

    public PdfGeneratorServiceTests(FakturaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static Company MakeCompany() => new()
    {
        OwnerFullName = "Erika Musterfrau",
        Address = "Hafenstraße 1",
        PostalCode = "20457",
        City = "Hamburg",
        BankName = "VR Bank Nord eG",
        BankAccount = "DE23217635420032798824",
        Email = "info@kuestencode.de"
    };

    [Fact]
    public void GeneratePdfWithCompany_RechnungMitVerknuepftenAbschlaegenUndZahlungen_ErzeugtNichtLeeresPdf()
    {
        var sourceInvoice = new Invoice
        {
            InvoiceNumber = "26-00456-RE",
            InvoiceDate = new DateTime(2026, 6, 1),
            CustomerId = 1,
            Items = [new InvoiceItem { Description = "Abschlag", Quantity = 1, UnitPrice = 10000m, VatRate = 19 }],
            Payments =
            [
                new InvoicePayment { Amount = 3900m, PaymentDate = new DateTime(2026, 6, 10) },
                new InvoicePayment { Amount = 8000m, PaymentDate = new DateTime(2026, 6, 20) }
            ]
        };

        var invoice = new Invoice
        {
            InvoiceNumber = "26-00500-RE",
            InvoiceDate = new DateTime(2026, 7, 1),
            CustomerId = 1,
            Customer = new Customer { Name = "Nordlicht Media", Address = "Hafenstraße 1", City = "Hamburg", PostalCode = "20457" },
            Items = [new InvoiceItem { Description = "Facharbeiterstunden", Quantity = 311.75m, UnitPrice = 65m, VatRate = 19 }],
            DownPayments =
            [
                new DownPayment
                {
                    Description = sourceInvoice.InvoiceNumber,
                    Amount = sourceInvoice.TotalGross,
                    SourceInvoiceId = 1,
                    SourceInvoice = sourceInvoice
                }
            ]
        };

        using var scope = _factory.Services.CreateScope();
        var pdfGeneratorService = scope.ServiceProvider.GetRequiredService<IPdfGeneratorService>();

        var pdfBytes = pdfGeneratorService.GeneratePdfWithCompany(invoice, MakeCompany());

        pdfBytes.Should().NotBeNull();
        pdfBytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateInvoicePdfAsync_AbschlagMitVerknuepfterAbschlagsrechnungAusDb_LaedtQuellrechnungUndErzeugtPdf()
    {
        int finalInvoiceId;

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<FakturaDbContext>();

            var sourceInvoice = new Invoice
            {
                InvoiceNumber = "26-00999-RE",
                InvoiceDate = new DateTime(2026, 6, 1),
                CustomerId = 1,
                Items = [new InvoiceItem { Description = "Abschlag", Quantity = 1, UnitPrice = 10000m, VatRate = 19 }],
                Payments = [new InvoicePayment { Amount = 11900m, PaymentDate = new DateTime(2026, 6, 10) }]
            };
            context.Invoices.Add(sourceInvoice);
            await context.SaveChangesAsync();

            var finalInvoice = new Invoice
            {
                InvoiceNumber = "26-01000-RE",
                InvoiceDate = new DateTime(2026, 7, 1),
                CustomerId = 1,
                Items = [new InvoiceItem { Description = "Facharbeiterstunden", Quantity = 10, UnitPrice = 65m, VatRate = 19 }],
                DownPayments =
                [
                    new DownPayment { Description = "1. Abschlagsrechnung", Amount = 11900m, SourceInvoiceId = sourceInvoice.Id }
                ]
            };
            context.Invoices.Add(finalInvoice);
            await context.SaveChangesAsync();

            finalInvoiceId = finalInvoice.Id;
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var pdfGeneratorService = scope.ServiceProvider.GetRequiredService<IPdfGeneratorService>();

            var pdfBytes = await pdfGeneratorService.GenerateInvoicePdfAsync(finalInvoiceId);

            pdfBytes.Should().NotBeNull();
            pdfBytes.Length.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public async Task GetDocumentPdfAsync_MitEingefrorenemBeleg_LiefertDasOriginal()
    {
        byte[] original = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31];
        int invoiceId;

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<FakturaDbContext>();
            var invoice = new Invoice
            {
                InvoiceNumber = "ALT-2025-0815",
                InvoiceDate = new DateTime(2025, 11, 3),
                CustomerId = 1,
                IsImported = true,
                Items = [new InvoiceItem { Description = "Offener Posten", Quantity = 1, UnitPrice = 100m, VatRate = 19 }],
                Attachments =
                [
                    new InvoiceAttachment { FileName = "original.pdf", ContentType = "application/pdf", Data = original, FileSize = original.Length, IsFrozenSnapshot = true }
                ]
            };
            context.Invoices.Add(invoice);
            await context.SaveChangesAsync();
            invoiceId = invoice.Id;
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var pdfBytes = await scope.ServiceProvider.GetRequiredService<IPdfGeneratorService>().GetDocumentPdfAsync(invoiceId);

            pdfBytes.Should().Equal(original);
        }
    }

    [Fact]
    public async Task GetDocumentPdfAsync_OhneEingefrorenenBeleg_RendertPdf()
    {
        int invoiceId;

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<FakturaDbContext>();
            var invoice = new Invoice
            {
                InvoiceNumber = "26-02000-RE",
                InvoiceDate = new DateTime(2026, 7, 1),
                CustomerId = 1,
                Items = [new InvoiceItem { Description = "Beratung", Quantity = 1, UnitPrice = 100m, VatRate = 19 }]
            };
            context.Invoices.Add(invoice);
            await context.SaveChangesAsync();
            invoiceId = invoice.Id;
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var pdfBytes = await scope.ServiceProvider.GetRequiredService<IPdfGeneratorService>().GetDocumentPdfAsync(invoiceId);

            pdfBytes.Length.Should().BeGreaterThan(0);
            pdfBytes.Take(4).Should().Equal(0x25, 0x50, 0x44, 0x46);
        }
    }
}
