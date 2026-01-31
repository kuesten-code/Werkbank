using Kuestencode.Werkbank.Offerte.Data.Repositories;
using Kuestencode.Werkbank.Offerte.Domain.Enums;
using Kuestencode.Werkbank.Offerte.Domain.Services;
using Kuestencode.Werkbank.Offerte.Services.Pdf;

namespace Kuestencode.Werkbank.Offerte.Services;

/// <summary>
/// Service zur Druckvorbereitung von Angeboten.
/// </summary>
public class OfferteDruckService : IOfferteDruckService
{
    private readonly IAngebotRepository _repository;
    private readonly IOffertePdfService _pdfService;
    private readonly AngebotStatusService _statusService;
    private readonly ILogger<OfferteDruckService> _logger;

    public OfferteDruckService(
        IAngebotRepository repository,
        IOffertePdfService pdfService,
        AngebotStatusService statusService,
        ILogger<OfferteDruckService> logger)
    {
        _repository = repository;
        _pdfService = pdfService;
        _statusService = statusService;
        _logger = logger;
    }

    public async Task<byte[]> DruckvorbereitungAsync(Guid angebotId)
    {
        _logger.LogInformation("Druckvorbereitung für Angebot {AngebotId}", angebotId);
        return await _pdfService.ErstelleAsync(angebotId);
    }

    public async Task MarkiereAlsGedrucktAsync(Guid angebotId)
    {
        var angebot = await _repository.GetByIdAsync(angebotId);
        if (angebot == null)
        {
            throw new InvalidOperationException($"Angebot mit ID {angebotId} nicht gefunden.");
        }

        angebot.GedrucktAm = DateTime.UtcNow;
        angebot.DruckAnzahl++;

        // Status zu Versendet ändern, wenn noch im Entwurf
        if (angebot.Status == AngebotStatus.Entwurf)
        {
            _statusService.Versenden(angebot);
            _logger.LogInformation(
                "Angebot {Angebotsnummer} Status zu Versendet geändert nach Druck",
                angebot.Angebotsnummer);
        }

        await _repository.UpdateAsync(angebot);

        _logger.LogInformation(
            "Angebot {Angebotsnummer} als gedruckt markiert (Anzahl: {DruckAnzahl})",
            angebot.Angebotsnummer, angebot.DruckAnzahl);
    }
}
