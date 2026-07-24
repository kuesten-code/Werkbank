using Kuestencode.Core.Models;
using Kuestencode.Faktura.Models;

namespace Kuestencode.Faktura.Services;

public class PreviewService : IPreviewService
{
    public Invoice GenerateSampleInvoice(Company company)
    {
        var invoice = new Invoice
        {
            Id = 999,
            InvoiceNumber = "RE-2025-001",
            InvoiceDate = new DateTime(2025, 1, 17),
            DueDate = new DateTime(2025, 1, 31),
            Customer = SampleCustomer(),
            CustomerId = SampleCustomer().Id,
            Items = SampleItems(company, 1m)
        };

        // Totals are computed automatically via [NotMapped] properties

        return invoice;
    }

    public Invoice GenerateSampleCreditNote(Company company)
    {
        var creditNote = new Invoice
        {
            Id = 999,
            Type = InvoiceType.CreditNote,
            InvoiceNumber = "GS-2025-001",
            InvoiceDate = new DateTime(2025, 1, 17),
            Customer = SampleCustomer(),
            CustomerId = SampleCustomer().Id,
            Items = SampleItems(company, -1m)
        };

        return creditNote;
    }

    private static Customer SampleCustomer() => new()
    {
        Id = 999,
        CustomerNumber = "K-2025-001",
        Name = "Musterfirma GmbH",
        Address = "Musterstraße 123",
        PostalCode = "12345",
        City = "Musterstadt",
        Email = "max@musterfirma.de"
    };

    private static List<InvoiceItem> SampleItems(Company company, decimal sign) =>
    [
        new InvoiceItem
        {
            Position = 1,
            Description = "Webentwicklung - Frontend Implementierung",
            Quantity = 8,
            UnitPrice = sign * 85.00m,
            VatRate = company.IsKleinunternehmer ? 0 : 19
        },
        new InvoiceItem
        {
            Position = 2,
            Description = "Backend API-Integration",
            Quantity = 5,
            UnitPrice = sign * 95.00m,
            VatRate = company.IsKleinunternehmer ? 0 : 19
        },
        new InvoiceItem
        {
            Position = 3,
            Description = "Projektmanagement & Konzeption",
            Quantity = 3,
            UnitPrice = sign * 75.00m,
            VatRate = company.IsKleinunternehmer ? 0 : 19
        }
    ];
}
