using Kuestencode.Shared.Contracts.Feedback;

namespace Kuestencode.Werkbank.Host.Models.Feedback;

/// <summary>
/// Kommentar im lokalen Cache (Rolle Client): eigene Antworten (Outbox) und vom Hub
/// gespiegelte öffentliche Kommentare.
/// </summary>
public class FeedbackClientComment : IFeedbackOutboxItem
{
    public int Id { get; set; }
    public int ClientReportId { get; set; }

    public Guid? ClientCommentId { get; set; }
    public int? HubCommentId { get; set; }

    public FeedbackCommentAuthor Author { get; set; }
    public string Text { get; set; } = "";
    public DateTime CreatedAt { get; set; }

    public FeedbackSyncState SyncState { get; set; } = FeedbackSyncState.Ausstehend;
    public int SendAttempts { get; set; }
    public DateTime? NextAttemptAt { get; set; }
    public string? LastError { get; set; }
}
