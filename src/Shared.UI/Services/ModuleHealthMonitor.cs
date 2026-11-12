using System.Net.Http.Json;
using Kuestencode.Shared.Contracts.Navigation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kuestencode.Shared.UI.Services;

/// <summary>
/// Registriert das Modul beim Start am Host und erneut, sobald der Host 60 Sekunden lang keinen
/// Health-Check mehr geschickt hat (z. B. nach einem Host-Neustart mit leerer Registry).
/// </summary>
public class ModuleHealthMonitor : BackgroundService
{
    public const string HostClientName = "werkbank-host-registration";

    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(10);

    private readonly ILogger<ModuleHealthMonitor> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _moduleName;
    private readonly Func<ModuleInfoDto> _getModuleInfo;
    private readonly object _lock = new();
    private DateTime _lastHealthCheckTime = DateTime.UtcNow;

    public ModuleHealthMonitor(
        ILogger<ModuleHealthMonitor> logger,
        IHttpClientFactory httpClientFactory,
        string moduleName,
        Func<ModuleInfoDto> getModuleInfo)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _moduleName = moduleName;
        _getModuleInfo = getModuleInfo;
    }

    public void RecordHealthCheck()
    {
        lock (_lock)
        {
            _lastHealthCheckTime = DateTime.UtcNow;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(InitialDelay, stoppingToken);
        await RegisterWithHostAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(CheckInterval, stoppingToken);
            await ReregisterIfHealthChecksStoppedAsync(stoppingToken);
        }
    }

    private async Task ReregisterIfHealthChecksStoppedAsync(CancellationToken stoppingToken)
    {
        lock (_lock)
        {
            if (DateTime.UtcNow - _lastHealthCheckTime <= HealthCheckTimeout)
                return;

            _lastHealthCheckTime = DateTime.UtcNow;
        }

        _logger.LogWarning("{ModuleName}: Kein Health-Check vom Host seit {Seconds} s, registriere neu",
            _moduleName, HealthCheckTimeout.TotalSeconds);
        await RegisterWithHostAsync(stoppingToken);
    }

    public async Task<bool> RegisterWithHostAsync(CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(HostClientName);
            var response = await client.PostAsJsonAsync("/api/modules/register", _getModuleInfo(), cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("{ModuleName}: beim Host registriert", _moduleName);
                return true;
            }

            _logger.LogWarning("{ModuleName}: Registrierung beim Host fehlgeschlagen. Status: {StatusCode}",
                _moduleName, response.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "{ModuleName}: Host für Registrierung nicht erreichbar", _moduleName);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "{ModuleName}: Zeitüberschreitung bei der Registrierung", _moduleName);
        }

        return false;
    }
}
