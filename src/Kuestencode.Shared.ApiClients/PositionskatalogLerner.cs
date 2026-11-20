using Kuestencode.Shared.Contracts.Host;
using Microsoft.Extensions.Logging;

namespace Kuestencode.Shared.ApiClients;

public interface IPositionskatalogLerner
{
    /// <summary>
    /// Übergibt Positionen an den Katalog im Host. Wirft nie: Das Speichern eines Dokuments
    /// darf nicht daran scheitern, dass der Host nicht erreichbar ist.
    /// </summary>
    Task<bool> MerkenAsync(IReadOnlyCollection<PositionsvorlageMerkenDto> positionen);
}

public class PositionskatalogLerner : IPositionskatalogLerner
{
    public const int PaketGroesse = 500;

    private readonly IHostApiClient _hostApiClient;
    private readonly ILogger<PositionskatalogLerner> _logger;

    public PositionskatalogLerner(IHostApiClient hostApiClient, ILogger<PositionskatalogLerner> logger)
    {
        _hostApiClient = hostApiClient;
        _logger = logger;
    }

    public async Task<bool> MerkenAsync(IReadOnlyCollection<PositionsvorlageMerkenDto> positionen)
    {
        try
        {
            foreach (var paket in positionen.Chunk(PaketGroesse))
                await _hostApiClient.MerkePositionsvorlagenAsync(paket.ToList()).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Positionen konnten nicht in den Positionskatalog übernommen werden");
            return false;
        }
    }
}
