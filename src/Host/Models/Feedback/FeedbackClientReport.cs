using Kuestencode.Core.Models;
using Kuestencode.Shared.Contracts.Feedback;

namespace Kuestencode.Werkbank.Host.Models.Feedback;

/// <summary>
/// Lokale Meldung der Kunden-Instanz (Rolle Client): zugleich Outbox-Eintrag (bis zum
/// erfolgreichen Senden) und Cache des vom Hub gespiegelten Status.
/// </summary>
public class FeedbackClientReport : BaseEntity, IFeedbackOutboxItem
{
    public Guid ClientReportId { get; set; }

    public FeedbackType Type { get; set; }
    public string? Module { get; set; }
    public string Title { get; set; } = "";
    public string? Expected { get; set; }
    public string Actual { get; set; } = "";
    public string ReporterName { get; set; } = "";
    public string PageUrl { get; set; } = "";
    public string AppVersion { get; set; } = "";

    public FeedbackSyncState SyncState { get; set; } = FeedbackSyncState.Ausstehend;
    public int SendAttempts { get; set; }
    public DateTime? NextAttemptAt { get; set; }
    public string? LastError { get; set; }

    public int? HubReportId { get; set; }
    public FeedbackStatus Status { get; set; } = FeedbackStatus.Neu;
    public int? DuplicateOfHubId { get; set; }

    public List<FeedbackClientComment> Comments { get; set; } = new();
    public List<FeedbackClientAttachment> Attachments { get; set; } = new();
}
