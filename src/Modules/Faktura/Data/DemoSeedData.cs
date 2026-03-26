using Kuestencode.Core.Models;
using Kuestencode.Faktura.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kuestencode.Faktura.Data;

/// <summary>
/// Minimaler DbContext für den Demo-Seed: schreibt Kunden direkt ins host-Schema,
/// ohne eine vollständige Abhängigkeit auf das Host-Projekt zu benötigen.
/// </summary>
internal class SeedHostDbContext : DbContext
{
    public SeedHostDbContext(DbContextOptions<SeedHostDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("host");
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CustomerNumber).IsUnique();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}

/// <summary>
/// Demo-Daten für Faktura. Legt Demo-Kunden (im Host-Schema) und Musterrechnungen
/// für Testzwecke und Vorführungen an.
/// </summary>
public static class DemoSeedData
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var fakturaContext = scope.ServiceProvider.GetRequiredService<FakturaDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<FakturaDbContext>>();

        // HostDbContext ist im Faktura-Prozess nicht per DI registriert —
        // wir bauen ihn direkt über die gleiche Connection-String auf.
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' nicht gefunden.");
        var hostOptions = new DbContextOptionsBuilder<SeedHostDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        using var hostContext = new SeedHostDbContext(hostOptions);

        // Nur seeden wenn keine Rechnungen vorhanden
        if (await fakturaContext.Invoices.AnyAsync())
        {
            logger.LogInformation("Faktura Demo-Daten bereits vorhanden, Seed übersprungen.");
            return;
        }

        logger.LogInformation("Faktura Demo-Daten werden angelegt...");

        // === Kunden anlegen (im host-Schema, wenn noch nicht vorhanden) ===

        var customerMueller = await GetOrCreateCustomerAsync(hostContext, "K-0001", new Customer
        {
            CustomerNumber = "K-0001",
            Name = "Müller & Partner Steuerberatungsgesellschaft mbH",
            Address = "Hauptstraße 42",
            PostalCode = "20095",
            City = "Hamburg",
            Country = "Deutschland",
            Email = "buchhaltung@mueller-steuerberatung.de",
            Phone = "040 1234567",
            Salutation = "Sehr geehrte Damen und Herren,"
        });

        var customerSchmidt = await GetOrCreateCustomerAsync(hostContext, "K-0002", new Customer
        {
            CustomerNumber = "K-0002",
            Name = "Schmidt Maschinenbau GmbH",
            Address = "Industrieweg 17",
            PostalCode = "22844",
            City = "Norderstedt",
            Country = "Deutschland",
            Email = "verwaltung@schmidt-maschinenbau.de",
            Phone = "040 9876543",
            Salutation = "Sehr geehrte Damen und Herren,"
        });

        var customerWeber = await GetOrCreateCustomerAsync(hostContext, "K-0003", new Customer
        {
            CustomerNumber = "K-0003",
            Name = "Dr. Andrea Weber",
            Address = "Alsterufer 15",
            PostalCode = "20354",
            City = "Hamburg",
            Country = "Deutschland",
            Email = "praxis@dr-weber-hamburg.de",
            Phone = "040 5551234",
            Salutation = "Sehr geehrte Frau Dr. Weber,"
        });

        var customerNordsoft = await GetOrCreateCustomerAsync(hostContext, "K-0004", new Customer
        {
            CustomerNumber = "K-0004",
            Name = "NordSoft Solutions AG",
            Address = "Spitalerstraße 4",
            PostalCode = "20095",
            City = "Hamburg",
            Country = "Deutschland",
            Email = "finanzen@nordsoft.de",
            Phone = "040 3334455",
            Salutation = "Sehr geehrte Damen und Herren,"
        });

        await hostContext.SaveChangesAsync();

        // === Rechnungen ===

        var invoices = new List<Invoice>
        {
            // --- Müller & Partner: Bezahlte Jahresrechnung 2025 ---
            new Invoice
            {
                InvoiceNumber = "RE-2025-0001",
                InvoiceDate = D(2025, 12, 15),
                ServicePeriodStart = D(2025, 10, 1),
                ServicePeriodEnd = D(2025, 12, 15),
                DueDate = D(2026, 1, 14),
                CustomerId = customerMueller.Id,
                Status = InvoiceStatus.Paid,
                PaidDate = D(2025, 12, 28),
                Notes = "Webseitenpflege Q4 2025",
                Items = new List<InvoiceItem>
                {
                    new InvoiceItem { Position = 1, Description = "Technische Wartung & Aktualisierung WordPress", Quantity = 4, UnitPrice = 95.00m, VatRate = 19m },
                    new InvoiceItem { Position = 2, Description = "SEO-Optimierung und Performance-Analyse", Quantity = 2, UnitPrice = 110.00m, VatRate = 19m },
                    new InvoiceItem { Position = 3, Description = "SSL-Zertifikat Verlängerung", Quantity = 1, UnitPrice = 49.00m, VatRate = 19m }
                }
            },

            // --- Müller & Partner: Bezahlte Rechnung Jan 2026 ---
            new Invoice
            {
                InvoiceNumber = "RE-2026-0001",
                InvoiceDate = D(2026, 1, 10),
                ServicePeriodStart = D(2026, 1, 1),
                ServicePeriodEnd = D(2026, 1, 31),
                DueDate = D(2026, 2, 9),
                CustomerId = customerMueller.Id,
                Status = InvoiceStatus.Paid,
                PaidDate = D(2026, 2, 5),
                Notes = "Monatliche Wartungspauschale",
                Items = new List<InvoiceItem>
                {
                    new InvoiceItem { Position = 1, Description = "Monatliche Server-Betreuung und Monitoring", Quantity = 1, UnitPrice = 180.00m, VatRate = 19m },
                    new InvoiceItem { Position = 2, Description = "Sicherheits-Updates und Plugin-Pflege", Quantity = 1, UnitPrice = 95.00m, VatRate = 19m }
                }
            },

            // --- Schmidt Maschinenbau: Großauftrag, Teilzahlungen ---
            new Invoice
            {
                InvoiceNumber = "RE-2026-0002",
                InvoiceDate = D(2026, 1, 20),
                ServicePeriodStart = D(2025, 11, 1),
                ServicePeriodEnd = D(2026, 1, 20),
                DueDate = D(2026, 2, 19),
                CustomerId = customerSchmidt.Id,
                Status = InvoiceStatus.Paid,
                PaidDate = D(2026, 2, 10),
                Notes = "Entwicklung Auftragsmanagement-Portal, Phase 1",
                DiscountType = DiscountType.Percentage,
                DiscountValue = 5m,
                Items = new List<InvoiceItem>
                {
                    new InvoiceItem { Position = 1, Description = "Anforderungsanalyse und Konzeption", Quantity = 12, UnitPrice = 120.00m, VatRate = 19m },
                    new InvoiceItem { Position = 2, Description = "Backend-Entwicklung (ASP.NET Core, PostgreSQL)", Quantity = 24, UnitPrice = 120.00m, VatRate = 19m },
                    new InvoiceItem { Position = 3, Description = "Frontend-Entwicklung (Blazor)", Quantity = 18, UnitPrice = 110.00m, VatRate = 19m },
                    new InvoiceItem { Position = 4, Description = "Testing & Qualitätssicherung", Quantity = 8, UnitPrice = 95.00m, VatRate = 19m },
                    new InvoiceItem { Position = 5, Description = "Deployment & Inbetriebnahme", Quantity = 4, UnitPrice = 120.00m, VatRate = 19m }
                },
                DownPayments = new List<DownPayment>
                {
                    new DownPayment
                    {
                        Description = "Anzahlung 40 % gemäß Angebot AN-2025-0047",
                        Amount = 2800.00m,
                        PaymentDate = D(2025, 11, 10)
                    }
                }
            },

            // --- Schmidt Maschinenbau: Phase 2, versendet, noch offen ---
            new Invoice
            {
                InvoiceNumber = "RE-2026-0003",
                InvoiceDate = D(2026, 2, 28),
                ServicePeriodStart = D(2026, 1, 21),
                ServicePeriodEnd = D(2026, 2, 28),
                DueDate = D(2026, 3, 30),
                CustomerId = customerSchmidt.Id,
                Status = InvoiceStatus.Sent,
                EmailSentAt = DT(2026, 2, 28, 14, 30, 0),
                EmailSentTo = "verwaltung@schmidt-maschinenbau.de",
                EmailSendCount = 1,
                Notes = "Entwicklung Auftragsmanagement-Portal, Phase 2",
                Items = new List<InvoiceItem>
                {
                    new InvoiceItem { Position = 1, Description = "Erweiterung Berichts-Modul und Export-Funktionen", Quantity = 16, UnitPrice = 120.00m, VatRate = 19m },
                    new InvoiceItem { Position = 2, Description = "Benutzerverwaltung und Rechtesystem", Quantity = 10, UnitPrice = 120.00m, VatRate = 19m },
                    new InvoiceItem { Position = 3, Description = "Schulung und Dokumentation", Quantity = 6, UnitPrice = 95.00m, VatRate = 19m }
                }
            },

            // --- Dr. Weber: Bezahlte Website-Erstellung ---
            new Invoice
            {
                InvoiceNumber = "RE-2025-0002",
                InvoiceDate = D(2025, 11, 5),
                DueDate = D(2025, 12, 5),
                CustomerId = customerWeber.Id,
                Status = InvoiceStatus.Paid,
                PaidDate = D(2025, 11, 25),
                Notes = "Neugestaltung Praxis-Website inkl. Online-Terminbuchung",
                Items = new List<InvoiceItem>
                {
                    new InvoiceItem { Position = 1, Description = "Design-Konzept und Wireframing", Quantity = 6, UnitPrice = 100.00m, VatRate = 19m },
                    new InvoiceItem { Position = 2, Description = "WordPress-Entwicklung (responsiv, barrierefrei)", Quantity = 14, UnitPrice = 110.00m, VatRate = 19m },
                    new InvoiceItem { Position = 3, Description = "Integration Online-Terminbuchung (Doctolib-Schnittstelle)", Quantity = 4, UnitPrice = 120.00m, VatRate = 19m },
                    new InvoiceItem { Position = 4, Description = "Texterstellung und Bildoptimierung", Quantity = 3, UnitPrice = 85.00m, VatRate = 19m }
                }
            },

            // --- Dr. Weber: Jährliche Wartungspauschale 2026 ---
            new Invoice
            {
                InvoiceNumber = "RE-2026-0004",
                InvoiceDate = D(2026, 1, 2),
                ServicePeriodStart = D(2026, 1, 1),
                ServicePeriodEnd = D(2026, 12, 31),
                DueDate = D(2026, 1, 31),
                CustomerId = customerWeber.Id,
                Status = InvoiceStatus.Paid,
                PaidDate = D(2026, 1, 20),
                Notes = "Jährliche Wartungs- und Betreuungspauschale 2026",
                Items = new List<InvoiceItem>
                {
                    new InvoiceItem { Position = 1, Description = "Jahrespauschale Website-Hosting & -Betreuung", Quantity = 1, UnitPrice = 540.00m, VatRate = 19m },
                    new InvoiceItem { Position = 2, Description = "Support-Kontingent (12 × 1 Stunde)", Quantity = 12, UnitPrice = 80.00m, VatRate = 19m }
                }
            },

            // --- NordSoft: Großprojekt SaaS-Plattform, verschiedene Positionen ---
            new Invoice
            {
                InvoiceNumber = "RE-2026-0005",
                InvoiceDate = D(2026, 2, 14),
                ServicePeriodStart = D(2026, 1, 1),
                ServicePeriodEnd = D(2026, 2, 14),
                DueDate = D(2026, 3, 16),
                CustomerId = customerNordsoft.Id,
                Status = InvoiceStatus.Sent,
                EmailSentAt = DT(2026, 2, 14, 9, 0, 0),
                EmailSentTo = "finanzen@nordsoft.de",
                EmailSendCount = 1,
                Notes = "API-Entwicklung und Microservices-Architektur",
                Items = new List<InvoiceItem>
                {
                    new InvoiceItem { Position = 1, Description = "Architekturberatung und Systemdesign", Quantity = 8, UnitPrice = 135.00m, VatRate = 19m },
                    new InvoiceItem { Position = 2, Description = "REST-API Entwicklung (ASP.NET Core)", Quantity = 32, UnitPrice = 120.00m, VatRate = 19m },
                    new InvoiceItem { Position = 3, Description = "Datenbankmodellierung und Migration (PostgreSQL)", Quantity = 12, UnitPrice = 115.00m, VatRate = 19m },
                    new InvoiceItem { Position = 4, Description = "Integrationstest und Dokumentation (OpenAPI)", Quantity = 10, UnitPrice = 95.00m, VatRate = 19m }
                }
            },

            // --- NordSoft: Laufende Rechnung (Entwurf) ---
            new Invoice
            {
                InvoiceNumber = "RE-2026-0006",
                InvoiceDate = D(2026, 3, 1),
                ServicePeriodStart = D(2026, 2, 15),
                ServicePeriodEnd = D(2026, 3, 1),
                DueDate = D(2026, 3, 31),
                CustomerId = customerNordsoft.Id,
                Status = InvoiceStatus.Draft,
                Notes = "DevOps & CI/CD-Pipeline Aufbau — in Bearbeitung",
                Items = new List<InvoiceItem>
                {
                    new InvoiceItem { Position = 1, Description = "Docker-Containerisierung aller Services", Quantity = 6, UnitPrice = 120.00m, VatRate = 19m },
                    new InvoiceItem { Position = 2, Description = "GitHub Actions CI/CD-Pipeline Konfiguration", Quantity = 8, UnitPrice = 115.00m, VatRate = 19m },
                    new InvoiceItem { Position = 3, Description = "Monitoring & Alerting (Grafana/Prometheus)", Quantity = 5, UnitPrice = 120.00m, VatRate = 19m }
                }
            },

            // --- Müller & Partner: Überfällige Rechnung Feb 2026 ---
            new Invoice
            {
                InvoiceNumber = "RE-2026-0007",
                InvoiceDate = D(2026, 2, 1),
                ServicePeriodStart = D(2026, 2, 1),
                ServicePeriodEnd = D(2026, 2, 28),
                DueDate = D(2026, 3, 3),
                CustomerId = customerMueller.Id,
                Status = InvoiceStatus.Overdue,
                EmailSentAt = DT(2026, 2, 1, 10, 0, 0),
                EmailSentTo = "buchhaltung@mueller-steuerberatung.de",
                EmailSendCount = 2,
                Notes = "Monatliche Wartungspauschale Februar — Zahlung überfällig",
                Items = new List<InvoiceItem>
                {
                    new InvoiceItem { Position = 1, Description = "Monatliche Server-Betreuung und Monitoring", Quantity = 1, UnitPrice = 180.00m, VatRate = 19m },
                    new InvoiceItem { Position = 2, Description = "Sicherheits-Updates und Plugin-Pflege", Quantity = 1, UnitPrice = 95.00m, VatRate = 19m }
                }
            }
        };

        fakturaContext.Invoices.AddRange(invoices);
        await fakturaContext.SaveChangesAsync();

        logger.LogInformation(
            "Faktura Demo-Daten angelegt: {CustomerCount} Kunden, {InvoiceCount} Rechnungen.",
            4, invoices.Count);
    }

    // PostgreSQL timestamptz erwartet UTC — alle Datums-Literale über diese Helfer anlegen.
    private static DateTime D(int y, int m, int d) =>
        DateTime.SpecifyKind(new DateTime(y, m, d), DateTimeKind.Utc);
    private static DateTime DT(int y, int mo, int d, int h, int mi, int s) =>
        DateTime.SpecifyKind(new DateTime(y, mo, d, h, mi, s), DateTimeKind.Utc);

    private static async Task<Customer> GetOrCreateCustomerAsync(SeedHostDbContext context, string customerNumber, Customer newCustomer)
    {
        var existing = await context.Customers
            .FirstOrDefaultAsync(c => c.CustomerNumber == customerNumber);

        if (existing != null)
            return existing;

        context.Customers.Add(newCustomer);
        return newCustomer;
    }
}
