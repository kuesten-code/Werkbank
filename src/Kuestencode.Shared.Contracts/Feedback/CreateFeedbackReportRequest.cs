namespace Kuestencode.Shared.Contracts.Feedback;

/// <summary>
/// JSON-Teil ("report") des multipart-Requests an POST /api/v1/reports. Screenshots werden
/// als zusätzliche Datei-Teile ("files") übertragen.
/// </summary>
public class CreateFeedbackReportRequest
{
    /// <summary>Von der Kunden-Instanz erzeugt; macht das Senden idempotent.</summary>
    public Guid ClientReportId { get; set; }
    public FeedbackType Type { get; set; }
    public string? Module { get; set; }
    public string Title { get; set; } = "";
    public string? Expected { get; set; }
    public string Actual { get; set; } = "";
    public string ReporterName { get; set; } = "";
    public string PageUrl { get; set; } = "";
    public string AppVersion { get; set; } = "";
}
