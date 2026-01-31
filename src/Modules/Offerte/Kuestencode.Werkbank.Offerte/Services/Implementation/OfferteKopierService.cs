using Kuestencode.Werkbank.Offerte.Data.Repositories;
using Kuestencode.Werkbank.Offerte.Domain.Entities;
using Kuestencode.Werkbank.Offerte.Domain.Enums;
using Kuestencode.Werkbank.Offerte.Domain.Interfaces;

namespace Kuestencode.Werkbank.Offerte.Services;

/// <summary>
/// Service zum Kopieren von Angeboten.
/// </summary>
public class OfferteKopierService : IOfferteKopierService
{
    private readonly IAngebotRepository _repository;
    private readonly IAngebotsnummernService _nummernService;
    private readonly ILogger<OfferteKopierService> _logger;

    public OfferteKopierService(
        IAngebotRepository repository,
        IAngebotsnummernService nummernService,
        ILogger<OfferteKopierService> logger)
    {
        _repository = repository;
        _nummernService = nummernService;
        _logger = logger;
    }

    public async Task<Angebot> KopiereAsync(Guid angebotId, int gueltigkeitsTage = 14)
    {
        var original = await _repository.GetByIdAsync(angebotId);
        if (original == null)
        {
            throw new InvalidOperationException($"Angebot mit ID {angebotId} nicht gefunden.");
        }

        var neueNummer = await _nummernService.NaechsteNummerAsync();
        var jetzt = DateTime.UtcNow;

        var kopie = new Angebot
        {
            Id = Guid.NewGuid(),
            Angebotsnummer = neueNummer,
            KundeId = original.KundeId,
            Status = AngebotStatus.Entwurf,
            Erstelldatum = jetzt,
            GueltigBis = jetzt.AddDays(gueltigkeitsTage),
            Referenz = original.Referenz,
            Bemerkungen = original.Bemerkungen,
            Einleitung = original.Einleitung,
            Schlusstext = original.Schlusstext,
            Positionen = new List<Angebotsposition>()
        };

        // Positionen kopieren
        var posNr = 1;
        foreach (var originalPosition in original.Positionen.OrderBy(p => p.Position))
        {
            kopie.Positionen.Add(new Angebotsposition
            {
                Id = Guid.NewGuid(),
                AngebotId = kopie.Id,
                Position = posNr++,
                Text = originalPosition.Text,
                Menge = originalPosition.Menge,
                Einzelpreis = originalPosition.Einzelpreis,
                Steuersatz = originalPosition.Steuersatz,
                Rabatt = originalPosition.Rabatt
            });
        }

        await _repository.AddAsync(kopie);

        _logger.LogInformation(
            "Angebot {OriginalNummer} kopiert zu {KopieNummer}",
            original.Angebotsnummer, kopie.Angebotsnummer);

        return kopie;
    }
}
