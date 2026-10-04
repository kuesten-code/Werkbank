using Kuestencode.Core.Models;
using Kuestencode.Shared.Contracts.Feedback;

namespace Kuestencode.Werkbank.Host.Models.Feedback;

/// <summary>Eine beim Hub eingegangene Meldung (Rolle Hub).</summary>
public class FeedbackReport : BaseEntity
{
    public int InstanceId { get; set; }
    public Guid ClientReportId { get; set; }

    public FeedbackType Type { get; set; }
    public string? Module { get; set; }
    public string Title { get; set; } = "";
    public string? Expected { get; set; }
    public string Actual { get; set; } = "";

    public string ReporterName { get; set; } = "";
    public string PageUrl { get; set; } = "";
    public string AppVersion { get; set; } = "";

    public FeedbackStatus Status { get; set; } = FeedbackStatus.Neu;
    public int? DuplicateOfId { get; set; }
    public string? GithubIssueUrl { get; set; }

    public FeedbackInstance Instance { get; set; } = null!;
    public List<FeedbackComment> Comments { get; set; } = new();
    public List<FeedbackAttachment> Attachments { get; set; } = new();
}
