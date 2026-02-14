using Kuestencode.Werkbank.Recepta.Domain.Entities;
using Kuestencode.Werkbank.Recepta.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kuestencode.Werkbank.Recepta.Data;

/// <summary>
/// Demo-Daten für Recepta. Erzeugt Lieferanten und Beispielbelege
/// zum Testen und Vorführen der Anwendung.
/// </summary>
public static class DemoSeedData
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReceptaDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ReceptaDbContext>>();

        // Nur seeden wenn keine Lieferanten vorhanden
        if (await context.Suppliers.AnyAsync())
        {
            logger.LogInformation("Recepta Demo-Daten bereits vorhanden, Seed übersprungen.");
            return;
        }

        logger.LogInformation("Recepta Demo-Daten werden angelegt...");

        // === Lieferanten ===

        var supplierHetzner = new Supplier
        {
            Id = Guid.NewGuid(),
            SupplierNumber = "L-0001",
            Name = "Hetzner Online GmbH",
            Address = "Industriestr. 25",
            PostalCode = "91710",
            City = "Gunzenhausen",
            Country = "DE",
            Email = "info@hetzner.com",
            TaxId = "DE812871812",
            Iban = "DE72760700120750379000",
            Bic = "DEUTDEMM760",
            Notes = "Hosting & Cloud-Server"
        };

        var supplierDigitalocean = new Supplier
        {
            Id = Guid.NewGuid(),
            SupplierNumber = "L-0002",
            Name = "DigitalOcean LLC",
            Address = "101 6th Ave",
            PostalCode = "10013",
            City = "New York",
            Country = "US",
            Email = "billing@digitalocean.com",
            Notes = "Cloud-Infrastruktur"
        };

        var supplierBueromarkt = new Supplier
        {
            Id = Guid.NewGuid(),
            SupplierNumber = "L-0003",
            Name = "Büromarkt Böttcher AG",
            Address = "Berliner Str. 100",
            PostalCode = "07743",
            City = "Jena",
            Country = "DE",
            Email = "service@bueromarkt.de",
            TaxId = "DE164557033",
            Iban = "DE89370400440532013000",
            Bic = "COBADEFFXXX",
            Notes = "Bürobedarf"
        };

        var supplierTelekom = new Supplier
        {
            Id = Guid.NewGuid(),
            SupplierNumber = "L-0004",
            Name = "Deutsche Telekom AG",
            Address = "Friedrich-Ebert-Allee 140",
            PostalCode = "53113",
            City = "Bonn",
            Country = "DE",
            TaxId = "DE123456789",
            Iban = "DE08370501980000057000",
            Bic = "COLSDE33XXX",
            Notes = "Internet & Telefon"
        };

        var supplierAdobe = new Supplier
        {
            Id = Guid.NewGuid(),
            SupplierNumber = "L-0005",
            Name = "Adobe Systems Software Ireland Ltd",
            Address = "4-6 Riverwalk, Citywest Business Campus",
            PostalCode = "D24",
            City = "Dublin",
            Country = "IE",
            Email = "billing@adobe.com",
            TaxId = "IE9692928F",
            Notes = "Software-Lizenzen (Creative Cloud)"
        };

        context.Suppliers.AddRange(supplierHetzner, supplierDigitalocean, supplierBueromarkt, supplierTelekom, supplierAdobe);

        // === Belege ===

        var documents = new List<Document>
        {
            // Hetzner - Server-Hosting (monatlich, verschiedene Status)
            new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "ER-2026-0001",
                SupplierId = supplierHetzner.Id,
                InvoiceNumber = "H-2026-1001",
                InvoiceDate = new DateOnly(2026, 1, 1),
                DueDate = new DateOnly(2026, 1, 15),
                AmountNet = 45.38m,
                TaxRate = 19m,
                AmountTax = 8.62m,
                AmountGross = 54.00m,
                Category = DocumentCategory.Material,
                Status = DocumentStatus.Paid,
                Notes = "Dedicated Server CX41, Januar 2026"
            },
            new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "ER-2026-0002",
                SupplierId = supplierHetzner.Id,
                InvoiceNumber = "H-2026-1002",
                InvoiceDate = new DateOnly(2026, 2, 1),
                DueDate = new DateOnly(2026, 2, 15),
                AmountNet = 45.38m,
                TaxRate = 19m,
                AmountTax = 8.62m,
                AmountGross = 54.00m,
                Category = DocumentCategory.Material,
                Status = DocumentStatus.Booked,
                Notes = "Dedicated Server CX41, Februar 2026"
            },

            // DigitalOcean - Droplets
            new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "ER-2026-0003",
                SupplierId = supplierDigitalocean.Id,
                InvoiceNumber = "DO-INV-2026-0042",
                InvoiceDate = new DateOnly(2026, 1, 31),
                DueDate = new DateOnly(2026, 2, 28),
                AmountNet = 28.00m,
                TaxRate = 0m,
                AmountTax = 0m,
                AmountGross = 28.00m,
                Category = DocumentCategory.Material,
                Status = DocumentStatus.Paid,
                Notes = "Droplet + Spaces, Januar 2026 (Reverse-Charge)"
            },

            // Büromarkt - Bürobedarf
            new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "ER-2026-0004",
                SupplierId = supplierBueromarkt.Id,
                InvoiceNumber = "BM-7712345",
                InvoiceDate = new DateOnly(2026, 1, 20),
                DueDate = new DateOnly(2026, 2, 20),
                AmountNet = 84.03m,
                TaxRate = 19m,
                AmountTax = 15.97m,
                AmountGross = 100.00m,
                Category = DocumentCategory.Office,
                Status = DocumentStatus.Paid,
                Notes = "Druckerpapier, Toner, Kugelschreiber"
            },
            new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "ER-2026-0005",
                SupplierId = supplierBueromarkt.Id,
                InvoiceNumber = "BM-7734567",
                InvoiceDate = new DateOnly(2026, 2, 10),
                DueDate = new DateOnly(2026, 3, 10),
                AmountNet = 210.08m,
                TaxRate = 19m,
                AmountTax = 39.92m,
                AmountGross = 250.00m,
                Category = DocumentCategory.Office,
                Status = DocumentStatus.Draft,
                Notes = "Neuer Monitor-Ständer, USB-Hub, Kabel"
            },

            // Telekom - Internet
            new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "ER-2026-0006",
                SupplierId = supplierTelekom.Id,
                InvoiceNumber = "T-2026-0001-8842",
                InvoiceDate = new DateOnly(2026, 1, 5),
                DueDate = new DateOnly(2026, 1, 20),
                AmountNet = 33.61m,
                TaxRate = 19m,
                AmountTax = 6.39m,
                AmountGross = 40.00m,
                Category = DocumentCategory.Office,
                Status = DocumentStatus.Paid,
                Notes = "Business-Internet 100/40, Januar 2026"
            },
            new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "ER-2026-0007",
                SupplierId = supplierTelekom.Id,
                InvoiceNumber = "T-2026-0002-8842",
                InvoiceDate = new DateOnly(2026, 2, 5),
                DueDate = new DateOnly(2026, 2, 20),
                AmountNet = 33.61m,
                TaxRate = 19m,
                AmountTax = 6.39m,
                AmountGross = 40.00m,
                Category = DocumentCategory.Office,
                Status = DocumentStatus.Booked,
                Notes = "Business-Internet 100/40, Februar 2026"
            },

            // Adobe - Software
            new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "ER-2026-0008",
                SupplierId = supplierAdobe.Id,
                InvoiceNumber = "ADB-EU-2026-00112233",
                InvoiceDate = new DateOnly(2026, 1, 15),
                DueDate = new DateOnly(2026, 2, 15),
                AmountNet = 59.49m,
                TaxRate = 19m,
                AmountTax = 11.30m,
                AmountGross = 70.79m,
                Category = DocumentCategory.Material,
                Status = DocumentStatus.Paid,
                Notes = "Creative Cloud Abo, Januar 2026"
            },

            // Noch ein Draft-Beleg
            new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "ER-2026-0009",
                SupplierId = supplierHetzner.Id,
                InvoiceNumber = "H-2026-1003",
                InvoiceDate = DateOnly.FromDateTime(DateTime.Today),
                DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(14)),
                AmountNet = 126.05m,
                TaxRate = 19m,
                AmountTax = 23.95m,
                AmountGross = 150.00m,
                Category = DocumentCategory.Material,
                Status = DocumentStatus.Draft,
                Notes = "Zusätzlicher Storage Box BX20, einmalige Einrichtung"
            }
        };

        context.Documents.AddRange(documents);

        await context.SaveChangesAsync();

        logger.LogInformation(
            "Recepta Demo-Daten angelegt: {SupplierCount} Lieferanten, {DocumentCount} Belege.",
            5, documents.Count);
    }
}
