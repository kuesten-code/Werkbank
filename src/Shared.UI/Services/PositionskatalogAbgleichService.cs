using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.Contracts.Host;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kuestencode.Shared.UI.Services;

/// <summary>
/// Liefert die Positionen eines Moduls, die in den gemeinsamen Positionskatalog gehören.
/// </summary>
public interface IPositionskatalogQuelle
{
    Task<List<PositionsvorlageMerkenDto>> GetPositionenAsync();
}

/// <summary>
/// Übergibt beim Modulstart den Altbestand an den Positionskatalog. Läuft bei jedem Start;
/// der Host übernimmt nur Neues oder Neueres, daher ist das wiederholbar.
/// </summary>
public class PositionskatalogAbgleichService : BackgroundService
{
    private static readonly TimeSpan[] Wartezeiten =
    [
        TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(5)
    ];

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PositionskatalogAbgleichService> _logger;

    public PositionskatalogAbgleichService(IServiceScopeFactory scopeFactory, ILogger<PositionskatalogAbgleichService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Host und eigene Migrationen brauchen nach dem Start einen Moment.
        foreach (var wartezeit in Wartezeiten)
        {
            await Task.Delay(wartezeit, stoppingToken);
            if (await AbgleichenAsync())
                return;
        }

        _logger.LogWarning("Abgleich des Positionskatalogs nach {Versuche} Versuchen aufgegeben", Wartezeiten.Length);
    }

    public async Task<bool> AbgleichenAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var quelle = scope.ServiceProvider.GetRequiredService<IPositionskatalogQuelle>();
            var lerner = scope.ServiceProvider.GetRequiredService<IPositionskatalogLerner>();

            var positionen = await quelle.GetPositionenAsync();
            return await lerner.MerkenAsync(positionen);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Positionen für den Positionskatalog konnten nicht gelesen werden");
            return false;
        }
    }
}
