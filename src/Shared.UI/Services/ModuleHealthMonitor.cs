using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using Kuestencode.Shared.Contracts.Navigation;

namespace Kuestencode.Shared.UI.Services;

/// <summary>
/// Background service that monitors when the last health check from Host occurred.
/// If no health check is received within 60 seconds, the module re-registers itself with the Host.
/// </summary>
public class ModuleHealthMonitor : BackgroundService
{
    private readonly ILogger<ModuleHealthMonitor> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _moduleName;
    private readonly Func<ModuleInfoDto> _getModuleInfo;
    private DateTime _lastHealthCheckTime = DateTime.UtcNow;
    private readonly TimeSpan _healthCheckTimeout = TimeSpan.FromSeconds(60);
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(10);
    private static readonly object _lock = new();

    public ModuleHealthMonitor(
        ILogger<ModuleHealthMonitor> logger,
        IConfiguration configuration,
        string moduleName,
        Func<ModuleInfoDto> getModuleInfo)
    {
        _logger = logger;
        _configuration = configuration;
        _moduleName = moduleName;
        _getModuleInfo = getModuleInfo;
    }

    /// <summary>
    /// Call this method whenever a health check request is received
    /// </summary>
    public void RecordHealthCheck()
    {
        lock (_lock)
        {
            _lastHealthCheckTime = DateTime.UtcNow;
            _logger.LogDebug("{ModuleName}: Health check recorded at {Time}", _moduleName, _lastHealthCheckTime);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("{ModuleName}: Health monitor started", _moduleName);

        // Initial delay to let the module fully start
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndReregisterIfNeededAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ModuleName}: Error in health monitor", _moduleName);
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task CheckAndReregisterIfNeededAsync(CancellationToken stoppingToken)
    {
        DateTime lastCheck;
        lock (_lock)
        {
            lastCheck = _lastHealthCheckTime;
        }

        var timeSinceLastCheck = DateTime.UtcNow - lastCheck;

        if (timeSinceLastCheck > _healthCheckTimeout)
        {
            _logger.LogWarning(
                "{ModuleName}: No health check received for {Seconds} seconds. Re-registering with Host...",
                _moduleName,
                timeSinceLastCheck.TotalSeconds);

            await ReregisterWithHostAsync(stoppingToken);

            // Reset the timer after re-registration attempt
            lock (_lock)
            {
                _lastHealthCheckTime = DateTime.UtcNow;
            }
        }
    }

    private async Task ReregisterWithHostAsync(CancellationToken stoppingToken)
    {
        try
        {
            var hostUrl = _configuration.GetValue<string>("ServiceUrls:Host") ?? "http://localhost:8080";
            using var client = new HttpClient { BaseAddress = new Uri(hostUrl) };
            client.Timeout = TimeSpan.FromSeconds(10);

            var moduleInfo = _getModuleInfo();

            var response = await client.PostAsJsonAsync("/api/modules/register", moduleInfo, stoppingToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("{ModuleName}: Successfully re-registered with Host", _moduleName);
            }
            else
            {
                _logger.LogWarning(
                    "{ModuleName}: Failed to re-register with Host. Status: {StatusCode}",
                    _moduleName,
                    response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ModuleName}: Error re-registering with Host", _moduleName);
        }
    }
}
