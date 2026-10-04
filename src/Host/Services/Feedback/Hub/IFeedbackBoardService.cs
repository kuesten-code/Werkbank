using Kuestencode.Shared.Contracts.Feedback;
using Kuestencode.Werkbank.Host.Models.Feedback;

namespace Kuestencode.Werkbank.Host.Services.Feedback.Hub;

public record FeedbackBoardFilter(int? InstanceId = null, FeedbackType? Type = null, string? Module = null);

/// <summary>Bearbeitung der Meldungen im Hub (Kanban und Detailansicht, nur Admin).</summary>
public interface IFeedbackBoardService
{
    Task<List<FeedbackReport>> GetReportsAsync(FeedbackBoardFilter filter);
    Task<List<string>> GetModulesAsync();
    Task<FeedbackReport?> GetReportAsync(int reportId);

    Task SetStatusAsync(int reportId, FeedbackStatus status);

    /// <exception cref="FeedbackValidationException">Bei leerem oder zu langem Text.</exception>
    Task AddHubCommentAsync(int reportId, string text, bool isInternal);

    /// <summary>Verweist auf die Original-Meldung und schließt die Meldung als Abgelehnt.</summary>
    /// <exception cref="FeedbackValidationException">Wenn das Original nicht existiert oder ungültig ist.</exception>
    Task MarkAsDuplicateAsync(int reportId, int originalReportId);

    Task<(FeedbackAttachment Attachment, Stream Content)?> OpenAttachmentAsync(int attachmentId);
}
