namespace Kuestencode.Werkbank.Host.Services.Feedback.Client;

/// <summary>
/// Weckt <see cref="FeedbackSyncBackgroundService"/> sofort auf, wenn ein neuer Outbox-Eintrag
/// angelegt wurde. Singleton, damit der scoped Service und der Hintergrunddienst dieselbe
/// Instanz teilen.
/// </summary>
public interface IFeedbackOutboxSignal
{
    void Signal();

    /// <returns>true, wenn das Signal (und nicht das Timeout) das Warten beendet hat.</returns>
    Task<bool> WaitAsync(TimeSpan timeout, CancellationToken ct);
}

public class FeedbackOutboxSignal : IFeedbackOutboxSignal
{
    private readonly SemaphoreSlim _semaphore = new(0, 1);

    public void Signal()
    {
        try
        {
            _semaphore.Release();
        }
        catch (SemaphoreFullException)
        {
            // Bereits signalisiert — ein Aufwecken genügt.
        }
    }

    public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken ct) => _semaphore.WaitAsync(timeout, ct);
}
