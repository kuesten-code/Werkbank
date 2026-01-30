using Kuestencode.Shared.Contracts.Offerte;
using Kuestencode.Werkbank.Offerte.Data.Repositories;
using Kuestencode.Werkbank.Offerte.Domain.Enums;

namespace Kuestencode.Werkbank.Offerte.Services;

/// <summary>
/// Service zur Überführung von Angeboten in Rechnungen.
/// </summary>
public class OfferteUeberfuehrungService : IOfferteUeberfuehrungService
{
    private readonly IAngebotRepository _repository;
    private readonly ILogger<OfferteUeberfuehrungService> _logger;

    public OfferteUeberfuehrungService(
        IAngebotRepository repository,
        ILogger<OfferteUeberfuehrungService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<RechnungErstellungDto> InRechnungUeberfuehrenAsync(Guid angebotId)
    {
        var angebot = await _repository.GetByIdAsync(angebotId);
        if (angebot == null)
        {
            throw new InvalidOperationException($"Angebot mit ID {angebotId} nicht gefunden.");
        }

        // Nur angenommene Angebote können in Rechnungen überführt werden
        if (angebot.Status != AngebotStatus.Angenommen)
        {
            throw new InvalidOperationException(
                $"Nur angenommene Angebote können in Rechnungen überführt werden. " +
                $"Aktueller Status: {angebot.Status}");
        }

        var dto = new RechnungErstellungDto
        {
            KundeId = angebot.KundeId,
            Referenz = $"Angebot {angebot.Angebotsnummer}",
            Positionen = angebot.Positionen
                .OrderBy(p => p.Position)
                .Select(p => new RechnungspositionDto
                {
                    Text = p.Text,
                    Menge = p.Menge,
                    Einzelpreis = p.Einzelpreis,
                    Steuersatz = p.Steuersatz,
                    Rabatt = p.Rabatt
                })
                .ToList()
        };

        _logger.LogInformation(
            "Angebot {Angebotsnummer} zur Rechnungserstellung vorbereitet",
            angebot.Angebotsnummer);

        return dto;
    }
}
