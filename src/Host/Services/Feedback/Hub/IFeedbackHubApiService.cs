using Kuestencode.Shared.Contracts.Feedback;

namespace Kuestencode.Werkbank.Host.Services.Feedback.Hub;

/// <summary>
/// Operationen der Hub-API für Kunden-Instanzen. Jede Methode arbeitet ausschließlich auf
/// Meldungen der übergebenen Instanz (Instanz-Isolation).
/// </summary>
public interface IFeedbackHubApiService
{
    /// <summary>
    /// Legt eine Meldung an. Ist die ClientReportId für die Instanz bereits bekannt, wird die
    /// vorhandene Meldung zurückgegeben (<c>Created = false</c>) und nichts dupliziert.
    /// </summary>
    /// <exception cref="FeedbackValidationException">Bei ungültigen Feldern oder Dateien.</exception>
    Task<(FeedbackReportDto Report, bool Created)> CreateReportAsync(
        int instanceId, CreateFeedbackReportRequest request, IReadOnlyList<FeedbackUpload> uploads, CancellationToken ct = default);

    Task<FeedbackSyncResponse> GetChangesAsync(int instanceId, DateTime? since);

    /// <returns>null, wenn die Meldung nicht existiert oder einer anderen Instanz gehört.</returns>
    /// <exception cref="FeedbackValidationException">Bei leerem oder zu langem Text.</exception>
    Task<FeedbackCommentDto?> AddCustomerCommentAsync(int instanceId, int reportId, CreateFeedbackCommentRequest request);
}
