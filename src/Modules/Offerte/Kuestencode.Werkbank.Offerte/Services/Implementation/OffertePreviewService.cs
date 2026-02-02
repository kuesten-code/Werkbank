using Kuestencode.Core.Models;
using Kuestencode.Werkbank.Offerte.Domain.Entities;
using Kuestencode.Werkbank.Offerte.Domain.Enums;

namespace Kuestencode.Werkbank.Offerte.Services;

/// <summary>
/// Service zum Generieren von Beispiel-Angeboten für die PDF-Vorschau.
/// </summary>
public interface IOffertePreviewService
{
    /// <summary>
    /// Erzeugt ein Beispiel-Angebot für die Vorschau.
    /// </summary>
    Angebot GenerateSampleAngebot(Company company);
}

/// <summary>
/// Implementation des OffertePreviewService.
/// </summary>
public class OffertePreviewService : IOffertePreviewService
{
    public Angebot GenerateSampleAngebot(Company company)
    {
        var angebot = new Angebot
        {
            Id = Guid.NewGuid(),
            Angebotsnummer = "AN-2025-001",
            KundeId = 999,
            Status = AngebotStatus.Entwurf,
            Erstelldatum = DateTime.UtcNow,
            GueltigBis = DateTime.UtcNow.AddDays(30),
            Referenz = "Projekt Webentwicklung",
            Einleitung = "Gerne unterbreiten wir Ihnen folgendes Angebot:",
            Schlusstext = "Wir freuen uns auf Ihre Rückmeldung und stehen für Rückfragen gerne zur Verfügung.",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Positionen = new List<Angebotsposition>
            {
                new Angebotsposition
                {
                    Id = Guid.NewGuid(),
                    Position = 1,
                    Text = "Webentwicklung - Frontend Implementierung",
                    Menge = 20,
                    Einzelpreis = 95.00m,
                    Steuersatz = company.IsKleinunternehmer ? 0 : 19
                },
                new Angebotsposition
                {
                    Id = Guid.NewGuid(),
                    Position = 2,
                    Text = "Backend API-Integration",
                    Menge = 15,
                    Einzelpreis = 95.00m,
                    Steuersatz = company.IsKleinunternehmer ? 0 : 19
                },
                new Angebotsposition
                {
                    Id = Guid.NewGuid(),
                    Position = 3,
                    Text = "Design-Konzept und Prototyping",
                    Menge = 1,
                    Einzelpreis = 550.00m,
                    Steuersatz = company.IsKleinunternehmer ? 0 : 19
                }
            }
        };

        return angebot;
    }
}
