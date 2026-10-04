namespace Kuestencode.Werkbank.Host.Services.Feedback.Client;

/// <summary>Exponentielles Backoff für die Outbox: 1, 2, 4, … Minuten, gedeckelt auf 60 Minuten.</summary>
public static class FeedbackBackoff
{
    private static readonly TimeSpan MaxDelay = TimeSpan.FromMinutes(60);

    public static TimeSpan DelayAfter(int failedAttempts)
    {
        var exponent = Math.Clamp(failedAttempts - 1, 0, 10);
        var delay = TimeSpan.FromMinutes(Math.Pow(2, exponent));
        return delay < MaxDelay ? delay : MaxDelay;
    }
}
