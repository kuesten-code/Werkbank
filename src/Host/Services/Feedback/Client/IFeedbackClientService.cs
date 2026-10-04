using Kuestencode.Shared.Contracts.Feedback;
using Kuestencode.Werkbank.Host.Models.Feedback;

namespace Kuestencode.Werkbank.Host.Services.Feedback.Client;

/// <summary>
/// UI-seitige Operationen der Kunden-Instanz. Meldungen und Antworten werden nur lokal in die
/// Outbox geschrieben — das Senden übernimmt <see cref="IFeedbackSyncService"/>.
/// </summary>
public interface IFeedbackClientService
{
    /// <exception cref="FeedbackValidationException">Bei ungültigen Feldern oder Dateien.</exception>
    Task<FeedbackClientReport> CreateReportAsync(CreateFeedbackReportRequest request, IReadOnlyList<FeedbackUpload> files);

    Task<List<FeedbackClientReport>> GetReportsAsync();

    /// <exception cref="FeedbackValidationException">Bei leerem oder zu langem Text.</exception>
    Task AddReplyAsync(int localReportId, string text);

    /// <summary>Prüft URL und Key; ein leerer Key bedeutet "gespeicherten Key verwenden".</summary>
    Task<(bool Success, string Message)> TestConnectionAsync(string? hubUrl, string? apiKey);
}
