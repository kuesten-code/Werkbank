namespace Kuestencode.Werkbank.Host.Models.Feedback;

public class FeedbackAttachment
{
    public int Id { get; set; }
    public int ReportId { get; set; }

    /// <summary>Bereinigter Original-Dateiname, nur zur Anzeige.</summary>
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Size { get; set; }

    /// <summary>Relativer, serverseitig erzeugter Pfad unterhalb des Feedback-Speicherordners.</summary>
    public string StoragePath { get; set; } = "";
}
