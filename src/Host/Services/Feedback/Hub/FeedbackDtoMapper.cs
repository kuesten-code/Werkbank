using Kuestencode.Shared.Contracts.Feedback;
using Kuestencode.Werkbank.Host.Models.Feedback;

namespace Kuestencode.Werkbank.Host.Services.Feedback.Hub;

/// <summary>
/// Einzige Stelle, an der Hub-Meldungen für Kunden-Instanzen serialisiert werden — interne
/// Kommentare werden hier zentral herausgefiltert.
/// </summary>
public static class FeedbackDtoMapper
{
    public static FeedbackReportDto ToDto(FeedbackReport report) => new()
    {
        Id = report.Id,
        ClientReportId = report.ClientReportId,
        Type = report.Type,
        Module = report.Module,
        Title = report.Title,
        Status = report.Status,
        DuplicateOfId = report.DuplicateOfId,
        CreatedAt = report.CreatedAt,
        UpdatedAt = report.UpdatedAt,
        Comments = report.Comments
            .Where(c => !c.IsInternal)
            .OrderBy(c => c.CreatedAt)
            .Select(ToDto)
            .ToList()
    };

    public static FeedbackCommentDto ToDto(FeedbackComment comment) => new()
    {
        Id = comment.Id,
        ClientCommentId = comment.ClientCommentId,
        Author = comment.Author,
        Text = comment.Text,
        CreatedAt = comment.CreatedAt
    };
}
