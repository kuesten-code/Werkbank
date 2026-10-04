namespace Kuestencode.Shared.Contracts.Feedback;

public class FeedbackReportDto
{
    public int Id { get; set; }
    public Guid ClientReportId { get; set; }
    public FeedbackType Type { get; set; }
    public string? Module { get; set; }
    public string Title { get; set; } = "";
    public FeedbackStatus Status { get; set; }

    /// <summary>Hub-Nummer der Original-Meldung, falls als Duplikat geschlossen.</summary>
    public int? DuplicateOfId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>Enthält ausschließlich öffentliche (nicht-interne) Kommentare.</summary>
    public List<FeedbackCommentDto> Comments { get; set; } = new();
}

public class FeedbackCommentDto
{
    public int Id { get; set; }
    public Guid? ClientCommentId { get; set; }
    public FeedbackCommentAuthor Author { get; set; }
    public string Text { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class FeedbackSyncResponse
{
    /// <summary>Zeitpunkt der Abfrage auf dem Hub; als nächstes "since" verwenden.</summary>
    public DateTime ServerTime { get; set; }
    public List<FeedbackReportDto> Reports { get; set; } = new();
}

public class FeedbackInstanceInfoDto
{
    public string Name { get; set; } = "";
}
