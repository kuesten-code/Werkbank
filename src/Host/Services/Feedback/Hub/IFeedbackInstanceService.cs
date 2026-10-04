using Kuestencode.Werkbank.Host.Models.Feedback;

namespace Kuestencode.Werkbank.Host.Services.Feedback.Hub;

public interface IFeedbackInstanceService
{
    Task<List<FeedbackInstance>> GetInstancesAsync();

    /// <summary>Legt eine Instanz an. Der Klartext-Key wird nur hier zurückgegeben und nie gespeichert.</summary>
    Task<(FeedbackInstance Instance, string ApiKey)> CreateInstanceAsync(string name);

    Task<string> RegenerateApiKeyAsync(int instanceId);
    Task SetActiveAsync(int instanceId, bool isActive);

    Task<FeedbackInstance?> FindActiveByApiKeyAsync(string apiKey);
}
