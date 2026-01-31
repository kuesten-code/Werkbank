using Kuestencode.Werkbank.Offerte.Data.Repositories;
using Kuestencode.Werkbank.Offerte.Domain.Services;

namespace Kuestencode.Werkbank.Offerte.Services;

/// <summary>
/// Service zur Prüfung und Aktualisierung abgelaufener Angebote.
/// </summary>
public class AngebotAblaufService : IAngebotAblaufService
{
    private readonly IAngebotRepository _repository;
    private readonly AngebotStatusService _statusService;
    private readonly ILogger<AngebotAblaufService> _logger;

    public AngebotAblaufService(
        IAngebotRepository repository,
        AngebotStatusService statusService,
        ILogger<AngebotAblaufService> logger)
    {
        _repository = repository;
        _statusService = statusService;
        _logger = logger;
    }

    public async Task<int> PruefeUndAktualiereAbgelaufeneAsync()
    {
        var abgelaufene = await _repository.GetAbgelaufeneAsync();
        var count = 0;

        foreach (var angebot in abgelaufene)
        {
            try
            {
                _statusService.AlsAbgelaufenMarkieren(angebot);
                await _repository.UpdateAsync(angebot);
                count++;

                _logger.LogInformation(
                    "Angebot {Angebotsnummer} automatisch als abgelaufen markiert (GueltigBis: {GueltigBis})",
                    angebot.Angebotsnummer, angebot.GueltigBis);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Fehler beim Markieren von Angebot {Angebotsnummer} als abgelaufen",
                    angebot.Angebotsnummer);
            }
        }

        return count;
    }
}
