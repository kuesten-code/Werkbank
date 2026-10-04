namespace Kuestencode.Werkbank.Host.Services.Feedback.Client;

public record FeedbackHubConnection(string BaseUrl, string ApiKey);

/// <summary>
/// Fehler beim Aufruf des Hubs. <see cref="IsPermanent"/> = der Hub hat den Inhalt abgelehnt
/// (Validierung, Größe) — ein erneuter Versuch mit denselben Daten ist zwecklos.
/// </summary>
public class FeedbackHubException : Exception
{
    public bool IsPermanent { get; }

    public FeedbackHubException(string message, bool isPermanent, Exception? inner = null)
        : base(message, inner)
    {
        IsPermanent = isPermanent;
    }
}
