namespace Kuestencode.Werkbank.Host.Models.Feedback;

/// <summary>Gemeinsamer Outbox-Zustand von lokalen Meldungen und Kommentaren (Rolle Client).</summary>
public interface IFeedbackOutboxItem
{
    FeedbackSyncState SyncState { get; set; }
    int SendAttempts { get; set; }
    DateTime? NextAttemptAt { get; set; }
    string? LastError { get; set; }
}
