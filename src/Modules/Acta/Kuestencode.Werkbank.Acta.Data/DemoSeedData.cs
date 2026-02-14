using Kuestencode.Werkbank.Acta.Domain.Entities;
using Kuestencode.Werkbank.Acta.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kuestencode.Werkbank.Acta.Data;

/// <summary>
/// Demo-Daten für Acta. Erzeugt Projekte und Aufgaben
/// zum Testen und Vorführen der Anwendung.
/// CustomerId verweist auf Kunden im Host-Schema (int).
/// </summary>
public static class DemoSeedData
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ActaDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ActaDbContext>>();

        // Nur seeden wenn keine Projekte vorhanden
        if (await context.Projects.AnyAsync())
        {
            logger.LogInformation("Acta Demo-Daten bereits vorhanden, Seed übersprungen.");
            return;
        }

        logger.LogInformation("Acta Demo-Daten werden angelegt...");

        // === Projekt 1: Website-Relaunch (Aktiv, mit Aufgaben) ===

        var projectWebsite = new Project
        {
            Id = Guid.NewGuid(),
            ProjectNumber = "P-2026-0001",
            Name = "Website-Relaunch Nordlicht GmbH",
            Description = "Kompletter Relaunch der Unternehmenswebsite inkl. CMS-Migration, " +
                          "Responsive Design und SEO-Optimierung.",
            CustomerId = 1,
            Address = "Deichstraße 42",
            PostalCode = "20459",
            City = "Hamburg",
            Status = ProjectStatus.Active,
            StartDate = new DateOnly(2026, 1, 15),
            TargetDate = new DateOnly(2026, 4, 30),
            BudgetNet = 12500.00m,
            ExternalId = 1
        };

        var websiteTasks = new List<ProjectTask>
        {
            new ProjectTask
            {
                Id = Guid.NewGuid(),
                ProjectId = projectWebsite.Id,
                Title = "Anforderungsanalyse und Konzept",
                Notes = "Kick-Off-Meeting, Stakeholder-Interviews, Wireframes erstellen",
                Status = ProjectTaskStatus.Completed,
                SortOrder = 0,
                TargetDate = new DateOnly(2026, 1, 31),
                CompletedAt = new DateTime(2026, 1, 28, 14, 0, 0, DateTimeKind.Utc)
            },
            new ProjectTask
            {
                Id = Guid.NewGuid(),
                ProjectId = projectWebsite.Id,
                Title = "Design-Entwurf und Abstimmung",
                Notes = "Mockups in Figma, 2 Korrekturschleifen",
                Status = ProjectTaskStatus.Completed,
                SortOrder = 1,
                TargetDate = new DateOnly(2026, 2, 15),
                CompletedAt = new DateTime(2026, 2, 12, 10, 30, 0, DateTimeKind.Utc)
            },
            new ProjectTask
            {
                Id = Guid.NewGuid(),
                ProjectId = projectWebsite.Id,
                Title = "Frontend-Entwicklung",
                Notes = "Blazor-Komponenten, Responsive Layout, Dark Mode",
                Status = ProjectTaskStatus.Open,
                SortOrder = 2,
                TargetDate = new DateOnly(2026, 3, 15)
            },
            new ProjectTask
            {
                Id = Guid.NewGuid(),
                ProjectId = projectWebsite.Id,
                Title = "CMS-Migration (Content)",
                Notes = "Bestehende Inhalte aus WordPress migrieren",
                Status = ProjectTaskStatus.Open,
                SortOrder = 3,
                TargetDate = new DateOnly(2026, 3, 31)
            },
            new ProjectTask
            {
                Id = Guid.NewGuid(),
                ProjectId = projectWebsite.Id,
                Title = "SEO-Optimierung und Go-Live",
                Notes = "Meta-Tags, Sitemap, Redirect-Map, DNS-Umstellung",
                Status = ProjectTaskStatus.Open,
                SortOrder = 4,
                TargetDate = new DateOnly(2026, 4, 30)
            }
        };

        // === Projekt 2: App-Entwicklung (Aktiv, frühes Stadium) ===

        var projectApp = new Project
        {
            Id = Guid.NewGuid(),
            ProjectNumber = "P-2026-0002",
            Name = "Kundenportal-App",
            Description = "Mobile App (iOS/Android) als Kundenportal mit Auftragsübersicht, " +
                          "Dokumenten-Download und Push-Benachrichtigungen.",
            CustomerId = 1,
            Status = ProjectStatus.Active,
            StartDate = new DateOnly(2026, 2, 1),
            TargetDate = new DateOnly(2026, 8, 31),
            BudgetNet = 35000.00m,
            ExternalId = 2
        };

        var appTasks = new List<ProjectTask>
        {
            new ProjectTask
            {
                Id = Guid.NewGuid(),
                ProjectId = projectApp.Id,
                Title = "Technologie-Evaluation",
                Notes = "MAUI vs. Flutter vs. React Native evaluieren",
                Status = ProjectTaskStatus.Completed,
                SortOrder = 0,
                TargetDate = new DateOnly(2026, 2, 14),
                CompletedAt = new DateTime(2026, 2, 10, 16, 0, 0, DateTimeKind.Utc)
            },
            new ProjectTask
            {
                Id = Guid.NewGuid(),
                ProjectId = projectApp.Id,
                Title = "API-Schnittstelle definieren",
                Notes = "OpenAPI-Spec, Auth-Konzept (OAuth2), Rate-Limiting",
                Status = ProjectTaskStatus.Open,
                SortOrder = 1,
                TargetDate = new DateOnly(2026, 3, 1)
            },
            new ProjectTask
            {
                Id = Guid.NewGuid(),
                ProjectId = projectApp.Id,
                Title = "UI/UX-Prototyp",
                Notes = "Klickbarer Prototyp für Kundenpräsentation",
                Status = ProjectTaskStatus.Open,
                SortOrder = 2,
                TargetDate = new DateOnly(2026, 3, 31)
            },
            new ProjectTask
            {
                Id = Guid.NewGuid(),
                ProjectId = projectApp.Id,
                Title = "Backend-Entwicklung",
                Status = ProjectTaskStatus.Open,
                SortOrder = 3,
                TargetDate = new DateOnly(2026, 5, 31)
            },
            new ProjectTask
            {
                Id = Guid.NewGuid(),
                ProjectId = projectApp.Id,
                Title = "App-Entwicklung und Testing",
                Status = ProjectTaskStatus.Open,
                SortOrder = 4,
                TargetDate = new DateOnly(2026, 7, 31)
            },
            new ProjectTask
            {
                Id = Guid.NewGuid(),
                ProjectId = projectApp.Id,
                Title = "App Store Deployment",
                Notes = "Review-Prozess Apple + Google, Soft Launch",
                Status = ProjectTaskStatus.Open,
                SortOrder = 5,
                TargetDate = new DateOnly(2026, 8, 31)
            }
        };

        // === Projekt 3: IT-Beratung (Abgeschlossen) ===

        var projectBeratung = new Project
        {
            Id = Guid.NewGuid(),
            ProjectNumber = "P-2025-0003",
            Name = "IT-Infrastruktur-Audit",
            Description = "Sicherheitsaudit der bestehenden IT-Infrastruktur inkl. " +
                          "Penetrationstests, Schwachstellenanalyse und Handlungsempfehlungen.",
            CustomerId = 2,
            Address = "Hafenstraße 12",
            PostalCode = "24103",
            City = "Kiel",
            Status = ProjectStatus.Completed,
            StartDate = new DateOnly(2025, 11, 1),
            TargetDate = new DateOnly(2025, 12, 15),
            BudgetNet = 4800.00m,
            ExternalId = 3,
            CompletedAt = new DateTime(2025, 12, 12, 17, 0, 0, DateTimeKind.Utc)
        };

        var beratungTasks = new List<ProjectTask>
        {
            new ProjectTask
            {
                Id = Guid.NewGuid(),
                ProjectId = projectBeratung.Id,
                Title = "Netzwerk-Scan und Bestandsaufnahme",
                Status = ProjectTaskStatus.Completed,
                SortOrder = 0,
                CompletedAt = new DateTime(2025, 11, 8, 12, 0, 0, DateTimeKind.Utc)
            },
            new ProjectTask
            {
                Id = Guid.NewGuid(),
                ProjectId = projectBeratung.Id,
                Title = "Penetrationstests durchführen",
                Status = ProjectTaskStatus.Completed,
                SortOrder = 1,
                CompletedAt = new DateTime(2025, 11, 22, 15, 0, 0, DateTimeKind.Utc)
            },
            new ProjectTask
            {
                Id = Guid.NewGuid(),
                ProjectId = projectBeratung.Id,
                Title = "Abschlussbericht erstellen",
                Notes = "PDF-Bericht mit Risikobewertung und Maßnahmenplan",
                Status = ProjectTaskStatus.Completed,
                SortOrder = 2,
                CompletedAt = new DateTime(2025, 12, 12, 17, 0, 0, DateTimeKind.Utc)
            }
        };

        // === Projekt 4: Entwurf (noch nicht gestartet) ===

        var projectEntwurf = new Project
        {
            Id = Guid.NewGuid(),
            ProjectNumber = "P-2026-0004",
            Name = "E-Commerce Shopify-Integration",
            Description = "Anbindung des bestehenden ERP-Systems an Shopify inkl. " +
                          "Produktsynchronisation und Bestellimport.",
            CustomerId = 2,
            Status = ProjectStatus.Draft,
            BudgetNet = 8500.00m,
            ExternalId = 4
        };

        // === Projekt 5: Pausiert ===

        var projectPausiert = new Project
        {
            Id = Guid.NewGuid(),
            ProjectNumber = "P-2026-0005",
            Name = "Intranet-Portal Redesign",
            Description = "Modernisierung des internen Portals. Pausiert wegen " +
                          "Budgetfreigabe im nächsten Quartal.",
            CustomerId = 1,
            Status = ProjectStatus.Paused,
            StartDate = new DateOnly(2026, 1, 10),
            TargetDate = new DateOnly(2026, 6, 30),
            BudgetNet = 15000.00m,
            ExternalId = 5
        };

        var pausedTasks = new List<ProjectTask>
        {
            new ProjectTask
            {
                Id = Guid.NewGuid(),
                ProjectId = projectPausiert.Id,
                Title = "Anforderungsworkshop",
                Status = ProjectTaskStatus.Completed,
                SortOrder = 0,
                CompletedAt = new DateTime(2026, 1, 17, 11, 0, 0, DateTimeKind.Utc)
            },
            new ProjectTask
            {
                Id = Guid.NewGuid(),
                ProjectId = projectPausiert.Id,
                Title = "Technische Konzeption",
                Notes = "Auth-Lösung, Rollen/Rechte-Modell",
                Status = ProjectTaskStatus.Open,
                SortOrder = 1,
                TargetDate = new DateOnly(2026, 2, 28)
            }
        };

        // Alles speichern
        context.Projects.AddRange(projectWebsite, projectApp, projectBeratung, projectEntwurf, projectPausiert);
        context.Tasks.AddRange(websiteTasks);
        context.Tasks.AddRange(appTasks);
        context.Tasks.AddRange(beratungTasks);
        context.Tasks.AddRange(pausedTasks);

        await context.SaveChangesAsync();

        logger.LogInformation(
            "Acta Demo-Daten angelegt: {ProjectCount} Projekte, {TaskCount} Aufgaben.",
            5, websiteTasks.Count + appTasks.Count + beratungTasks.Count + pausedTasks.Count);
    }
}
