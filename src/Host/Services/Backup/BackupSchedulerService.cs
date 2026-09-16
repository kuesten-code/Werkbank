using Cronos;

namespace Kuestencode.Werkbank.Host.Services.Backup;

/// <summary>
/// Führt automatische Backups gemäß <see cref="Models.BackupSettings.Schedule"/> (Cron) aus.
/// </summary>
public class BackupSchedulerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackupSchedulerService> _logger;

    public BackupSchedulerService(IServiceProvider serviceProvider, ILogger<BackupSchedulerService> logger)
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
                using var scope = _serviceProvider.CreateScope();
                var backupService = scope.ServiceProvider.GetRequiredService<IBackupService>();
                var settings = await backupService.GetSettingsAsync();

                if (!settings.Enabled)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                    continue;
                }

                if (!CronExpression.TryParse(settings.Schedule, out var cron))
                {
                    _logger.LogError("Ungültiger Backup-Zeitplan '{Schedule}' – prüfe erneut in 5 Minuten", settings.Schedule);
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    continue;
                }

                var nextRun = cron.GetNextOccurrence(DateTime.UtcNow);
                if (!nextRun.HasValue)
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    continue;
                }

                var delay = nextRun.Value - DateTime.UtcNow;
                if (delay > TimeSpan.Zero)
                {
                    _logger.LogInformation("Nächstes automatisches Backup: {NextRun:u}", nextRun.Value);
                    await Task.Delay(delay, stoppingToken);
                }

                if (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Starte geplantes Backup");
                    await backupService.RunBackupAsync(isManual: false, ct: stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normaler Shutdown-Pfad.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler im Backup-Scheduler");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
