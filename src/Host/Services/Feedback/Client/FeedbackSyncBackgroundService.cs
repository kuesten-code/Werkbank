using Kuestencode.Werkbank.Host.Models.Feedback;

namespace Kuestencode.Werkbank.Host.Services.Feedback.Client;

/// <summary>
/// Arbeitet die Outbox ab (sofort nach einem neuen Eintrag, sonst alle 30 Sekunden für fällige
/// Retries) und ruft alle paar Minuten Status und Kommentare vom Hub ab. Läuft nur in der
/// Rolle Client; die Kunden-Instanz braucht dafür keine eingehende Erreichbarkeit.
/// </summary>
public class FeedbackSyncBackgroundService : BackgroundService
{
    private static readonly TimeSpan OutboxInterval = TimeSpan.FromSeconds(30);

    private readonly IServiceProvider _serviceProvider;
    private readonly IFeedbackModeState _modeState;
    private readonly IFeedbackOutboxSignal _outboxSignal;
    private readonly TimeSpan _pollInterval;
    private readonly ILogger<FeedbackSyncBackgroundService> _logger;

    public FeedbackSyncBackgroundService(
        IServiceProvider serviceProvider,
        IFeedbackModeState modeState,
        IFeedbackOutboxSignal outboxSignal,
        IConfiguration configuration,
        ILogger<FeedbackSyncBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _modeState = modeState;
        _outboxSignal = outboxSignal;
        _pollInterval = TimeSpan.FromMinutes(configuration.GetValue("Feedback:PollIntervalMinutes", 5));
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var lastPoll = DateTime.MinValue;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_modeState.Role == FeedbackRole.Client)
                {
                    var poll = DateTime.UtcNow - lastPoll >= _pollInterval;

                    using var scope = _serviceProvider.CreateScope();
                    var syncService = scope.ServiceProvider.GetRequiredService<IFeedbackSyncService>();
                    await syncService.SyncAsync(poll, stoppingToken);

                    if (poll)
                        lastPoll = DateTime.UtcNow;
                }

                await _outboxSignal.WaitAsync(OutboxInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normaler Shutdown-Pfad.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler im Feedback-Abgleich");
                await _outboxSignal.WaitAsync(OutboxInterval, stoppingToken);
            }
        }
    }
}
