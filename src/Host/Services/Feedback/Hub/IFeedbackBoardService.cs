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

    /// <summary>
    /// Mögliche Originale für ein Duplikat: alle anderen Meldungen, die nicht selbst als
    /// Duplikat markiert sind (die UI trennt nach eigenem und fremdem Kunden).
    /// </summary>
    Task<List<FeedbackReport>> GetDuplicateCandidatesAsync(int reportId);

    /// <summary>Meldungen anderer Kunden, die intern als Hub-Duplikat auf diese Meldung verweisen.</summary>
    Task<List<FeedbackReport>> GetHubDuplicatesOfAsync(int reportId);

    /// <summary>
    /// Verweist auf die Original-Meldung desselben Kunden und schließt die Meldung als Abgelehnt.
    /// Der Kunde sieht den Verweis.
    /// </summary>
    /// <exception cref="FeedbackValidationException">Wenn das Original nicht existiert, einem anderen Kunden gehört oder selbst ein Duplikat ist.</exception>
    Task MarkAsDuplicateAsync(int reportId, int originalReportId);

    /// <summary>
    /// Verknüpft die Meldung intern mit der Meldung eines anderen Kunden. Status und
    /// Kundenansicht bleiben unverändert.
    /// </summary>
    /// <exception cref="FeedbackValidationException">Wenn das Original nicht existiert, demselben Kunden gehört oder selbst ein Hub-Duplikat ist.</exception>
    Task MarkAsHubDuplicateAsync(int reportId, int originalReportId);

    Task RemoveHubDuplicateAsync(int reportId);

    Task<(FeedbackAttachment Attachment, Stream Content)?> OpenAttachmentAsync(int attachmentId);
}
