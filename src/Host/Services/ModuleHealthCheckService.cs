using Kuestencode.Shared.Contracts.Navigation;

namespace Kuestencode.Werkbank.Host.Services;

/// <summary>
/// Background service that periodically checks if registered modules are still alive.
/// Unregisters modules that fail health checks.
/// </summary>
public class ModuleHealthCheckService : BackgroundService
{
    private readonly IModuleRegistry _moduleRegistry;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ModuleHealthCheckService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _httpTimeout = TimeSpan.FromSeconds(5);

    public ModuleHealthCheckService(
        IModuleRegistry moduleRegistry,
        IHttpClientFactory httpClientFactory,
        ILogger<ModuleHealthCheckService> logger)
    {
        _moduleRegistry = moduleRegistry;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Module Health Check Service started");

        // Initial delay before first check
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckModulesHealthAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during module health check");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task CheckModulesHealthAsync(CancellationToken stoppingToken)
    {
        var modules = _moduleRegistry.GetAllModules();

        foreach (var module in modules)
        {
            if (string.IsNullOrEmpty(module.HealthCheckUrl))
            {
                _logger.LogDebug("Module {ModuleName} has no health check URL, skipping", module.ModuleName);
                continue;
            }

            var isHealthy = await CheckModuleHealthAsync(module, stoppingToken);

            if (!isHealthy)
            {
                _logger.LogWarning("Module {ModuleName} failed health check, unregistering", module.ModuleName);
                _moduleRegistry.UnregisterModule(module.ModuleName);
            }
        }
    }

    private async Task<bool> CheckModuleHealthAsync(ModuleInfoDto module, CancellationToken stoppingToken)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = _httpTimeout;

            var response = await client.GetAsync(module.HealthCheckUrl, stoppingToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Module {ModuleName} health check passed", module.ModuleName);
                return true;
            }

            _logger.LogWarning("Module {ModuleName} health check failed with status {StatusCode}",
                module.ModuleName, response.StatusCode);
            return false;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Module {ModuleName} health check timed out", module.ModuleName);
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning("Module {ModuleName} health check failed: {Message}",
                module.ModuleName, ex.Message);
            return false;
        }
    }
}
