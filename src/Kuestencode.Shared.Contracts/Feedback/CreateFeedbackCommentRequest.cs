namespace Kuestencode.Shared.Contracts.Feedback;

public class CreateFeedbackCommentRequest
{
    /// <summary>Von der Kunden-Instanz erzeugt; verhindert doppelte Kommentare bei Retries der Outbox.</summary>
    public Guid ClientCommentId { get; set; }
    public string Text { get; set; } = "";
}
