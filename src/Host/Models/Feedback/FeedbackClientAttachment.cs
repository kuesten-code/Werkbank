namespace Kuestencode.Werkbank.Host.Models.Feedback;

/// <summary>Lokal zwischengespeicherter Screenshot einer Meldung, bis diese gesendet ist (Rolle Client).</summary>
public class FeedbackClientAttachment
{
    public int Id { get; set; }
    public int ClientReportId { get; set; }

    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Size { get; set; }
    public string StoragePath { get; set; } = "";
}
