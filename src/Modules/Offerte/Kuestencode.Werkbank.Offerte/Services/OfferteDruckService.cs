using Kuestencode.Werkbank.Offerte.Data.Repositories;
using Kuestencode.Werkbank.Offerte.Services.Pdf;

namespace Kuestencode.Werkbank.Offerte.Services;

/// <summary>
/// Service zur Druckvorbereitung von Angeboten.
/// </summary>
public class OfferteDruckService : IOfferteDruckService
{
    private readonly IAngebotRepository _repository;
    private readonly IOffertePdfService _pdfService;
    private readonly ILogger<OfferteDruckService> _logger;

    public OfferteDruckService(
        IAngebotRepository repository,
        IOffertePdfService pdfService,
        ILogger<OfferteDruckService> logger)
    {
        _repository = repository;
        _pdfService = pdfService;
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

        await _repository.UpdateAsync(angebot);

        _logger.LogInformation(
            "Angebot {Angebotsnummer} als gedruckt markiert (Anzahl: {DruckAnzahl})",
            angebot.Angebotsnummer, angebot.DruckAnzahl);
    }
}
