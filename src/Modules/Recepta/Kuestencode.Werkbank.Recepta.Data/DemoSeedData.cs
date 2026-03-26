using Kuestencode.Werkbank.Recepta.Domain.Entities;
using Kuestencode.Werkbank.Recepta.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kuestencode.Werkbank.Recepta.Data;

/// <summary>
/// Demo-Daten für Recepta. Erzeugt Lieferanten und Beispielbelege
/// zum Testen und Vorführen der Anwendung.
/// Alle Beträge und Kategorien sind nach EÜR-Systematik (§ 4 Abs. 3 EStG) aufgebaut.
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
            Notes = "Cloud-Infrastruktur (Reverse-Charge)"
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

        var supplierStegmann = new Supplier
        {
            Id = Guid.NewGuid(),
            SupplierNumber = "L-0006",
            Name = "Stegmann & Söhne Steuerberatung GmbH",
            Address = "Neuer Wall 50",
            PostalCode = "20354",
            City = "Hamburg",
            Country = "DE",
            Email = "kanzlei@stegmann-steuer.de",
            Phone = "040 6667788",
            TaxId = "DE288123456",
            Iban = "DE21200400600628790200",
            Bic = "COBADEFFXXX",
            Notes = "Steuerberatung & Jahresabschluss"
        };

        var supplierEurosign = new Supplier
        {
            Id = Guid.NewGuid(),
            SupplierNumber = "L-0007",
            Name = "EuroSign GmbH",
            Address = "Hamburger Str. 12",
            PostalCode = "22083",
            City = "Hamburg",
            Country = "DE",
            Email = "info@eurosign.de",
            TaxId = "DE301234567",
            Iban = "DE55200700240011609890",
            Bic = "DEUTDEDB200",
            Notes = "Werbedruck & Visitenkarten"
        };

        var supplierDeutscheBahn = new Supplier
        {
            Id = Guid.NewGuid(),
            SupplierNumber = "L-0008",
            Name = "Deutsche Bahn AG",
            Address = "Potsdamer Platz 2",
            PostalCode = "10785",
            City = "Berlin",
            Country = "DE",
            TaxId = "DE811908475",
            Notes = "Dienstreisen Bahn"
        };

        var supplierHdv = new Supplier
        {
            Id = Guid.NewGuid(),
            SupplierNumber = "L-0009",
            Name = "HDV Haftpflicht-Versicherungsverein a.G.",
            Address = "Hindenburgufer 18",
            PostalCode = "24105",
            City = "Kiel",
            Country = "DE",
            Email = "service@hdv.de",
            TaxId = "DE135792468",
            Notes = "Berufshaftpflichtversicherung IT"
        };

        var supplierIhk = new Supplier
        {
            Id = Guid.NewGuid(),
            SupplierNumber = "L-0010",
            Name = "Handelskammer Hamburg",
            Address = "Adolphsplatz 1",
            PostalCode = "20457",
            City = "Hamburg",
            Country = "DE",
            Email = "service@hk24.de",
            TaxId = "DE118780938",
            Notes = "IHK-Beiträge"
        };

        context.Suppliers.AddRange(
            supplierHetzner, supplierDigitalocean, supplierBueromarkt, supplierTelekom,
            supplierAdobe, supplierStegmann, supplierEurosign, supplierDeutscheBahn,
            supplierHdv, supplierIhk
        );

        // === Belege (EÜR 2025/2026) ===

        var documents = new List<Document>
        {
            // ── Hetzner – Server-Hosting (monatlich) ──────────────────────────

            new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "ER-2025-0001",
                SupplierId = supplierHetzner.Id,
                InvoiceNumber = "H-2025-1001",
                InvoiceDate = new DateOnly(2025, 10, 1),
                DueDate = new DateOnly(2025, 10, 15),
                PaidDate = new DateOnly(2025, 10, 10),
                AmountNet = 45.38m,
                TaxRate = 19m,
                AmountTax = 8.62m,
                AmountGross = 54.00m,
                Category = DocumentCategory.Software,
                Status = DocumentStatus.Paid,
                Notes = "Dedicated Server CX41, Oktober 2025"
            },
            new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "ER-2025-0002",
                SupplierId = supplierHetzner.Id,
                InvoiceNumber = "H-2025-1002",
                InvoiceDate = new DateOnly(2025, 11, 1),
                DueDate = new DateOnly(2025, 11, 15),
                PaidDate = new DateOnly(2025, 11, 8),
                AmountNet = 45.38m,
                TaxRate = 19m,
                AmountTax = 8.62m,
                AmountGross = 54.00m,
                Category = DocumentCategory.Software,
                Status = DocumentStatus.Paid,
                Notes = "Dedicated Server CX41, November 2025"
            },
            new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "ER-2025-0003",
                SupplierId = supplierHetzner.Id,
                InvoiceNumber = "H-2025-1003",
                InvoiceDate = new DateOnly(2025, 12, 1),
                DueDate = new DateOnly(2025, 12, 15),
                PaidDate = new DateOnly(2025, 12, 9),
                AmountNet = 45.38m,
                TaxRate = 19m,
                AmountTax = 8.62m,
                AmountGross = 54.00m,
                Category = DocumentCategory.Software,
                Status = DocumentStatus.Paid,
                Notes = "Dedicated Server CX41, Dezember 2025"
            },
            new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "ER-2026-0001",
                SupplierId = supplierHetzner.Id,
                InvoiceNumber = "H-2026-1001",
                InvoiceDate = new DateOnly(2026, 1, 1),
                DueDate = new DateOnly(2026, 1, 15),
                PaidDate = new DateOnly(2026, 1, 8),
                AmountNet = 45.38m,
                TaxRate = 19m,
                AmountTax = 8.62m,
                AmountGross = 54.00m,
                Category = DocumentCategory.Software,
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
                PaidDate = new DateOnly(2026, 2, 10),
                AmountNet = 45.38m,
                TaxRate = 19m,
                AmountTax = 8.62m,
                AmountGross = 54.00m,
                Category = DocumentCategory.Software,
                Status = DocumentStatus.Paid,
                Notes = "Dedicated Server CX41, Februar 2026"
            },
            new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "ER-2026-0003",
                SupplierId = supplierHetzner.Id,
                InvoiceNumber = "H-2026-1003",
                InvoiceDate = new DateOnly(2026, 3, 1),
                DueDate = new DateOnly(2026, 3, 15),
                AmountNet = 45.38m,
                TaxRate = 19m,
                AmountTax = 8.62m,
                AmountGross = 54.00m,
                Category = DocumentCategory.Software,
                Status = DocumentStatus.Booked,
                Notes = "Dedicated Server CX41, März 2026"
            },

            // ── DigitalOcean – Droplets (Reverse-Charge, kein DE-USt) ─────────

            new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "ER-2026-0004",
                SupplierId = supplierDigitalocean.Id,
                InvoiceNumber = "DO-INV-2026-0042",
                InvoiceDate = new DateOnly(2026, 1, 31),
                DueDate = new DateOnly(2026, 2, 28),
                PaidDate = new DateOnly(2026, 2, 3),
                AmountNet = 28.00m,
                TaxRate = 0m,
                AmountTax = 0m,
                AmountGross = 28.00m,
                Category = DocumentCategory.Software,
                Status = DocumentStatus.Paid,
                Notes = "Droplet + Spaces, Januar 2026 (§ 13b UStG – Reverse-Charge)"
            },
            new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "ER-2026-0005",
                SupplierId = supplierDigitalocean.Id,
                InvoiceNumber = "DO-INV-2026-0087",
                InvoiceDate = new DateOnly(2026, 2, 28),
                DueDate = new DateOnly(2026, 3, 31),
                AmountNet = 28.00m,
                TaxRate = 0m,
                AmountTax = 0m,
                AmountGross = 28.00m,
                Category = DocumentCategory.Software,
                Status = DocumentStatus.Booked,
                Notes = "Droplet + Spaces, Februar 2026 (§ 13b UStG – Reverse-Charge)"
            },

            // ── Adobe – Creative Cloud (IE → Reverse-Charge) ─────────────────

            new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "ER-2026-0006",
                SupplierId = supplierAdobe.Id,
                InvoiceNumber = "ADB-EU-2026-00112233",
                InvoiceDate = new DateOnly(2026, 1, 15),
                DueDate = new DateOnly(2026, 2, 15),
                PaidDate = new DateOnly(2026, 1, 20),
                AmountNet = 59.49m,
                TaxRate = 0m,
                AmountTax = 0m,
                AmountGross = 59.49m,
                Category = DocumentCategory.Software,
                Status = DocumentStatus.Paid,
                Notes = "Creative Cloud All Apps, Januar 2026 (§ 13b UStG – Reverse-Charge, IE → DE)"
            },
            new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "ER-2026-0007",
                SupplierId = supplierAdobe.Id,
                InvoiceNumber = "ADB-EU-2026-00145678",
                InvoiceDate = new DateOnly(2026, 2, 15),
                DueDate = new DateOnly(2026, 3, 15),
                PaidDate = new DateOnly(2026, 2, 20),
                AmountNet = 59.49m,
                TaxRate = 0m,
                AmountTax = 0m,
                AmountGross = 59.49m,
                Category = DocumentCategory.Software,
                Status = DocumentStatus.Paid,
                Notes = "Creative Cloud All Apps, Februar 2026 (§ 13b UStG – Reverse-Charge)"
            },

            // ── Deutsche Telekom – Internet & Telefon ────────────────────────

            new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "ER-2025-0004",
                SupplierId = supplierTelekom.Id,
                InvoiceNumber = "T-2025-0010-8842",
                InvoiceDate = new DateOnly(2025, 10, 5),
                DueDate = new DateOnly(2025, 10, 20),
                PaidDate = new DateOnly(2025, 10, 12),
                AmountNet = 33.61m,
                TaxRate = 19m,
                AmountTax = 6.39m,
                AmountGross = 40.00m,
                Category = DocumentCategory.Phone,
                Status = DocumentStatus.Paid,
                Notes = "Business-Internet 250/50 + Festnetz-Flat, Oktober 2025"
            },
            new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "ER-2025-0005",
                SupplierId = supplierTelekom.Id,
                InvoiceNumber = "T-2025-0011-8842",
                InvoiceDate = new DateOnly(2025, 11, 5),
                DueDate = new DateOnly(2025, 11, 20),
                PaidDate = new DateOnly(2025, 11, 12),
                AmountNet = 33.61m,
                TaxRate = 19m,
                AmountTax = 6.39m,
                AmountGross = 40.00m,
                Category = DocumentCategory.Phone,
                Status = DocumentStatus.Paid,
                Notes = "Business-Internet 250/50 + Festnetz-Flat, November 2025"
            },
            new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "ER-2025-0006",
                SupplierId = supplierTelekom.Id,
                InvoiceNumber = "T-2025-0012-8842",
                InvoiceDate = new DateOnly(2025, 12, 5),
                DueDate = new DateOnly(2025, 12, 20),
                PaidDate = new DateOnly(2025, 12, 10),
                AmountNet = 33.61m,
                TaxRate = 19m,
                AmountTax = 6.39m,
                AmountGross = 40.00m,
                Category = DocumentCategory.Phone,
                Status = DocumentStatus.Paid,
                Notes = "Business-Internet 250/50 + Festnetz-Flat, Dezember 2025"
            },
            new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "ER-2026-0008",
                SupplierId = supplierTelekom.Id,
                InvoiceNumber = "T-2026-0001-8842",
                InvoiceDate = new DateOnly(2026, 1, 5),
                DueDate = new DateOnly(2026, 1, 20),
                PaidDate = new DateOnly(2026, 1, 12),
                AmountNet = 33.61m,
                TaxRate = 19m,
                AmountTax = 6.39m,
                AmountGross = 40.00m,
                Category = DocumentCategory.Phone,
                Status = DocumentStatus.Paid,
                Notes = "Business-Internet 250/50 + Festnetz-Flat, Januar 2026"
            },
            new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "ER-2026-0009",
                SupplierId = supplierTelekom.Id,
                InvoiceNumber = "T-2026-0002-8842",
                InvoiceDate = new DateOnly(2026, 2, 5),
                DueDate = new DateOnly(2026, 2, 20),
                PaidDate = new DateOnly(2026, 2, 12),
                AmountNet = 33.61m,
                TaxRate = 19m,
                AmountTax = 6.39m,
                AmountGross = 40.00m,
                Category = DocumentCategory.Phone,
                Status = DocumentStatus.Paid,
                Notes = "Business-Internet 250/50 + Festnetz-Flat, Februar 2026"
            },

            // ── Bürobedarf ────────────────────────────────────────────────────

            new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "ER-2025-0007",
                SupplierId = supplierBueromarkt.Id,
                InvoiceNumber = "BM-7645321",
                InvoiceDate = new DateOnly(2025, 10, 15),
                DueDate = new DateOnly(2025, 11, 14),
                PaidDate = new DateOnly(2025, 10, 28),
                AmountNet = 63.03m,
                TaxRate = 19m,
                AmountTax = 11.97m,
                AmountGross = 75.00m,
                Category = DocumentCategory.Office,
                Status = DocumentStatus.Paid,
                Notes = "Druckerpapier (5 Pakete), Toner-Kartusche, Haftnotizen"
            },
            new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "ER-2025-0008",
                SupplierId = supplierBueromarkt.Id,
                InvoiceNumber = "BM-7698412",
                InvoiceDate = new DateOnly(2025, 12, 10),
                DueDate = new DateOnly(2026, 1, 9),
                PaidDate = new DateOnly(2025, 12, 22),
                AmountNet = 84.03m,
                TaxRate = 19m,
                AmountTax = 15.97m,
                AmountGross = 100.00m,
                Category = DocumentCategory.Office,
                Status = DocumentStatus.Paid,
                Notes = "Schreibtisch-Organizer, Ordner, USB-Sticks (3×), Kopierpapier"
            },
            new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "ER-2026-0010",
                SupplierId = supplierBueromarkt.Id,
                InvoiceNumber = "BM-7712345",
                InvoiceDate = new DateOnly(2026, 1, 20),
                DueDate = new DateOnly(2026, 2, 20),
                PaidDate = new DateOnly(2026, 2, 4),
                AmountNet = 84.03m,
                TaxRate = 19m,
                AmountTax = 15.97m,
                AmountGross = 100.00m,
                Category = DocumentCategory.Office,
                Status = DocumentStatus.Paid,
                Notes = "Druckerpapier (10 Pakete), Toner, Kugelschreiber"
            },
            new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "ER-2026-0011",
                SupplierId = supplierBueromarkt.Id,
                InvoiceNumber = "BM-7734567",
                InvoiceDate = new DateOnly(2026, 2, 10),
                DueDate = new DateOnly(2026, 3, 12),
                AmountNet = 210.08m,
                TaxRate = 19m,
                AmountTax = 39.92m,
                AmountGross = 250.00m,
                Category = DocumentCategory.Office,
                Status = DocumentStatus.Draft,
                Notes = "Ergonomischer Monitor-Arm, USB-C Hub, Kabelmanagement-Set — Beleg prüfen"
            },

            // ── Steuerberatung ────────────────────────────────────────────────

            new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "ER-2025-0009",
                SupplierId = supplierStegmann.Id,
                InvoiceNumber = "SB-2025-0089",
                InvoiceDate = new DateOnly(2025, 10, 31),
                DueDate = new DateOnly(2025, 11, 30),
                PaidDate = new DateOnly(2025, 11, 20),
                AmountNet = 420.17m,
                TaxRate = 19m,
                AmountTax = 79.83m,
                AmountGross = 500.00m,
                Category = DocumentCategory.Fees,
                Status = DocumentStatus.Paid,
                Notes = "Laufende Buchhaltung Q3 2025, USt-Voranmeldung Oktober"
            },
            new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "ER-2025-0010",
                SupplierId = supplierStegmann.Id,
                InvoiceNumber = "SB-2025-0112",
                InvoiceDate = new DateOnly(2025, 12, 15),
                DueDate = new DateOnly(2026, 1, 14),
                PaidDate = new DateOnly(2025, 12, 29),
                AmountNet = 1344.54m,
                TaxRate = 19m,
                AmountTax = 255.46m,
                AmountGross = 1600.00m,
                Category = DocumentCategory.Fees,
                Status = DocumentStatus.Paid,
                Notes = "Jahresabschluss 2024 (EÜR), Steuererklärung 2024"
            },
            new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "ER-2026-0012",
                SupplierId = supplierStegmann.Id,
                InvoiceNumber = "SB-2026-0031",
                InvoiceDate = new DateOnly(2026, 1, 31),
                DueDate = new DateOnly(2026, 3, 2),
                PaidDate = new DateOnly(2026, 2, 18),
                AmountNet = 420.17m,
                TaxRate = 19m,
                AmountTax = 79.83m,
                AmountGross = 500.00m,
                Category = DocumentCategory.Fees,
                Status = DocumentStatus.Paid,
                Notes = "Laufende Buchhaltung Q4 2025, USt-Voranmeldung Januar 2026"
            },

            // ── Werbung / Marketing ───────────────────────────────────────────

            new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "ER-2025-0011",
                SupplierId = supplierEurosign.Id,
                InvoiceNumber = "ES-2025-5521",
                InvoiceDate = new DateOnly(2025, 11, 12),
                DueDate = new DateOnly(2025, 12, 12),
                PaidDate = new DateOnly(2025, 11, 28),
                AmountNet = 252.10m,
                TaxRate = 19m,
                AmountTax = 47.90m,
                AmountGross = 300.00m,
                Category = DocumentCategory.Marketing,
                Status = DocumentStatus.Paid,
                Notes = "Visitenkarten 500 Stk. (beidseitig), Briefpapier 250 Blatt, Kuvertdruck"
            },

            // ── Dienstreisen ──────────────────────────────────────────────────

            new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "ER-2025-0012",
                SupplierId = supplierDeutscheBahn.Id,
                InvoiceNumber = "DB-2025-HH-BER-1042",
                InvoiceDate = new DateOnly(2025, 11, 18),
                DueDate = new DateOnly(2025, 11, 18),
                PaidDate = new DateOnly(2025, 11, 18),
                AmountNet = 67.23m,
                TaxRate = 7m,
                AmountTax = 4.71m,
                AmountGross = 71.94m,
                Category = DocumentCategory.Travel,
                Status = DocumentStatus.Paid,
                Notes = "ICE Hamburg Hbf → Berlin Hbf und zurück, Kundentermin NordSoft Solutions AG"
            },
            new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "ER-2026-0013",
                SupplierId = supplierDeutscheBahn.Id,
                InvoiceNumber = "DB-2026-HH-MUC-0215",
                InvoiceDate = new DateOnly(2026, 2, 6),
                DueDate = new DateOnly(2026, 2, 6),
                PaidDate = new DateOnly(2026, 2, 6),
                AmountNet = 98.12m,
                TaxRate = 7m,
                AmountTax = 6.87m,
                AmountGross = 104.99m,
                Category = DocumentCategory.Travel,
                Status = DocumentStatus.Paid,
                Notes = "ICE Hamburg Hbf → München Hbf und zurück, Fachmesse BEIT 2026"
            },

            // ── Berufshaftpflicht (Versicherung) ──────────────────────────────

            new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "ER-2025-0013",
                SupplierId = supplierHdv.Id,
                InvoiceNumber = "HDV-2025-IT-88321",
                InvoiceDate = new DateOnly(2025, 10, 1),
                DueDate = new DateOnly(2025, 10, 31),
                PaidDate = new DateOnly(2025, 10, 5),
                AmountNet = 336.13m,
                TaxRate = 19m,
                AmountTax = 63.87m,
                AmountGross = 400.00m,
                Category = DocumentCategory.Insurance,
                Status = DocumentStatus.Paid,
                Notes = "Berufshaftpflichtversicherung IT-Dienstleister, Jahresprämie 2026 (Vorauszahlung)"
            },

            // ── IHK-Beitrag ────────────────────────────────────────────────────

            new Document
            {
                Id = Guid.NewGuid(),
                DocumentNumber = "ER-2026-0014",
                SupplierId = supplierIhk.Id,
                InvoiceNumber = "HK24-2026-0045123",
                InvoiceDate = new DateOnly(2026, 1, 15),
                DueDate = new DateOnly(2026, 3, 31),
                PaidDate = new DateOnly(2026, 2, 1),
                AmountNet = 168.07m,
                TaxRate = 19m,
                AmountTax = 31.93m,
                AmountGross = 200.00m,
                Category = DocumentCategory.Fees,
                Status = DocumentStatus.Paid,
                Notes = "IHK-Grundbeitrag + Umlage 2026"
            }
        };

        context.Documents.AddRange(documents);

        await context.SaveChangesAsync();

        logger.LogInformation(
            "Recepta Demo-Daten angelegt: {SupplierCount} Lieferanten, {DocumentCount} Belege.",
            10, documents.Count);
    }
}
