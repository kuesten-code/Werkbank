using Kuestencode.Werkbank.Host.Models.Feedback;

namespace Kuestencode.Werkbank.Host.Services.Feedback;

public interface IFeedbackSettingsService
{
    Task<FeedbackSettings> GetSettingsAsync();

    /// <summary>
    /// Speichert die Einstellungen. <paramref name="newApiKey"/> ersetzt den gespeicherten
    /// Key nur, wenn er nicht leer ist (der Key wird nie zurück in die UI gegeben).
    /// </summary>
    Task UpdateSettingsAsync(FeedbackSettings settings, string? newApiKey);

    Task<string?> GetApiKeyAsync();
    Task SetLastSyncAtAsync(DateTime serverTime);
}
