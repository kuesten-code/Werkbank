using System.Net.Http.Json;
using Kuestencode.Shared.Contracts.Locatio;

namespace Kuestencode.Werkbank.Saldo.Services;

/// <summary>
/// Ruft die Locatio-Buchungen per service-to-service HTTP ab. Ohne konfigurierte
/// <c>ServiceUrls:Locatio</c> ist Locatio nicht Teil der Instanz und liefert nichts.
/// </summary>
public class LocatioDataService : ILocatioDataService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LocatioDataService> _logger;

    public LocatioDataService(HttpClient httpClient, ILogger<LocatioDataService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<LocatioBuchungDto>> GetBuchungenAsync(DateOnly von, DateOnly bis)
    {
        if (_httpClient.BaseAddress == null)
            return [];

        try
        {
            var buchungen = await _httpClient.GetFromJsonAsync<List<LocatioBuchungDto>>(
                $"/api/locatio/saldo-buchungen?from={von:yyyy-MM-dd}&to={bis:yyyy-MM-dd}");
            return buchungen ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Laden der Locatio-Buchungen für {Von} - {Bis}", von, bis);
            return [];
        }
    }
}
