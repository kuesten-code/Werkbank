using Kuestencode.Shared.Contracts.Feedback;

namespace Kuestencode.Werkbank.Host.Services.Feedback.Client;

/// <summary>
/// HTTP-Zugriff der Kunden-Instanz auf die Hub-API. Alle Aufrufe gehen ausschließlich
/// ausgehend vom Kunden zum Hub. Fehler werden einheitlich als <see cref="FeedbackHubException"/> geworfen.
/// </summary>
public interface IFeedbackHubClient
{
    Task<FeedbackInstanceInfoDto> GetInstanceAsync(FeedbackHubConnection connection, CancellationToken ct = default);

    Task<FeedbackReportDto> CreateReportAsync(
        FeedbackHubConnection connection, CreateFeedbackReportRequest request,
        IReadOnlyList<(string FileName, string ContentType, byte[] Content)> files, CancellationToken ct = default);

    Task<FeedbackSyncResponse> GetChangesAsync(FeedbackHubConnection connection, DateTime? since, CancellationToken ct = default);

    Task<FeedbackCommentDto> AddCommentAsync(
        FeedbackHubConnection connection, int hubReportId, CreateFeedbackCommentRequest request, CancellationToken ct = default);
}
