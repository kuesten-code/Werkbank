using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Models.Feedback;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Host.Services.Feedback.Hub;

public class FeedbackInstanceService : IFeedbackInstanceService
{
    private readonly HostDbContext _context;

    public FeedbackInstanceService(HostDbContext context)
    {
        _context = context;
    }

    public Task<List<FeedbackInstance>> GetInstancesAsync() =>
        _context.FeedbackInstances.AsNoTracking().OrderBy(i => i.Name).ToListAsync();

    public async Task<(FeedbackInstance Instance, string ApiKey)> CreateInstanceAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name ist erforderlich.", nameof(name));

        var apiKey = FeedbackApiKey.Generate();
        var instance = new FeedbackInstance
        {
            Name = name.Trim(),
            ApiKeyHash = FeedbackApiKey.Hash(apiKey),
            IsActive = true
        };

        _context.FeedbackInstances.Add(instance);
        await _context.SaveChangesAsync();
        _context.Entry(instance).State = EntityState.Detached;
        return (instance, apiKey);
    }

    public async Task<string> RegenerateApiKeyAsync(int instanceId)
    {
        var instance = await FindTrackedAsync(instanceId);
        var apiKey = FeedbackApiKey.Generate();
        instance.ApiKeyHash = FeedbackApiKey.Hash(apiKey);
        await _context.SaveChangesAsync();
        return apiKey;
    }

    public async Task SetActiveAsync(int instanceId, bool isActive)
    {
        var instance = await FindTrackedAsync(instanceId);
        instance.IsActive = isActive;
        await _context.SaveChangesAsync();
    }

    public Task<FeedbackInstance?> FindActiveByApiKeyAsync(string apiKey)
    {
        var hash = FeedbackApiKey.Hash(apiKey);
        return _context.FeedbackInstances.AsNoTracking()
            .FirstOrDefaultAsync(i => i.ApiKeyHash == hash && i.IsActive);
    }

    private async Task<FeedbackInstance> FindTrackedAsync(int instanceId) =>
        await _context.FeedbackInstances.FindAsync(instanceId)
            ?? throw new InvalidOperationException($"Instanz {instanceId} nicht gefunden.");
}
