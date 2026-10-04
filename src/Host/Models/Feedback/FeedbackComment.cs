using Kuestencode.Shared.Contracts.Feedback;

namespace Kuestencode.Werkbank.Host.Models.Feedback;

public class FeedbackComment
{
    public int Id { get; set; }
    public int ReportId { get; set; }

    /// <summary>Nur bei Kunden-Kommentaren gesetzt (Idempotenz der Outbox).</summary>
    public Guid? ClientCommentId { get; set; }

    public FeedbackCommentAuthor Author { get; set; }
    public string Text { get; set; } = "";
    public bool IsInternal { get; set; }
    public DateTime CreatedAt { get; set; }
}
