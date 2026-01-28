using Kuestencode.Rapport.Data.Repositories;
using Kuestencode.Rapport.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kuestencode.Rapport.Services;

/// <summary>
/// Background service that auto-stops running timers after a configured threshold.
/// </summary>
public class AutoStopTimerHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AutoStopTimerHostedService> _logger;

    public AutoStopTimerHostedService(IServiceProvider serviceProvider, ILogger<AutoStopTimerHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndStopAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AutoStopTimerHostedService failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task CheckAndStopAsync(CancellationToken token)
    {
        using var scope = _serviceProvider.CreateScope();
        var settingsService = scope.ServiceProvider.GetRequiredService<SettingsService>();
        var timeEntryRepository = scope.ServiceProvider.GetRequiredService<TimeEntryRepository>();

        var settings = await settingsService.GetSettingsAsync();
        if (!settings.AutoStopTimerAfterHours.HasValue || settings.AutoStopTimerAfterHours.Value <= 0)
        {
            return;
        }

        var running = await timeEntryRepository.GetRunningEntryAsync();
        if (running == null)
        {
            return;
        }

        var maxHours = settings.AutoStopTimerAfterHours.Value;
        var limit = running.StartTime.AddHours(maxHours);
        var now = DateTime.UtcNow;

        if (now < limit)
        {
            return;
        }

        running.EndTime = limit;
        running.Status = TimeEntryStatus.Stopped;
        if (string.IsNullOrWhiteSpace(running.Description))
        {
            running.Description = $"Automatisch gestoppt nach {maxHours} h";
        }
        else if (!running.Description.Contains("Automatisch gestoppt", StringComparison.OrdinalIgnoreCase))
        {
            running.Description = running.Description.Trim() + $" (Automatisch gestoppt nach {maxHours} h)";
        }

        await timeEntryRepository.UpdateAsync(running);
        _logger.LogInformation("Auto-stopped running timer {EntryId} after {Hours} hours.", running.Id, maxHours);
    }
}
