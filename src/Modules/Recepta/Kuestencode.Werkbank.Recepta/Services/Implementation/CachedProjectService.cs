using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.Contracts.Acta;
using Microsoft.Extensions.Caching.Memory;

namespace Kuestencode.Werkbank.Recepta.Services;

/// <summary>
/// Cached wrapper for Acta project loading via Host API.
/// Caches for 5 minutes to reduce cross-module API calls.
/// </summary>
public class CachedProjectService : ICachedProjectService
{
    private readonly IHostApiClient _hostApiClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedProjectService> _logger;

    private const string ProjectsCacheKey = "acta_projects";
    private const string AvailabilityCacheKey = "acta_available";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan EmptyCacheDuration = TimeSpan.FromSeconds(15);

    public CachedProjectService(
        IHostApiClient hostApiClient,
        IMemoryCache cache,
        ILogger<CachedProjectService> logger)
    {
        _hostApiClient = hostApiClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<ActaProjectDto>> GetProjectsAsync()
    {
        if (_cache.TryGetValue(ProjectsCacheKey, out List<ActaProjectDto>? cached) && cached != null)
        {
            return cached;
        }

        var projects = await _hostApiClient.GetActaProjectsAsync();
        if (projects.Count > 0)
        {
            _cache.Set(ProjectsCacheKey, projects, CacheDuration);
            _cache.Set(AvailabilityCacheKey, true, CacheDuration);
        }
        else
        {
            // Kurzen Cache setzen, damit beim nächsten Seitenaufruf erneut versucht wird
            _logger.LogDebug("Acta-Projekte nicht verfügbar oder leer, kurzer Cache");
            _cache.Set(AvailabilityCacheKey, false, EmptyCacheDuration);
        }
        return projects;
    }

    public async Task<bool> IsActaAvailableAsync()
    {
        if (_cache.TryGetValue(AvailabilityCacheKey, out bool available))
        {
            return available;
        }

        // Projekte laden setzt den Availability-Cache mit, daher hier direkt nutzen
        var projects = await GetProjectsAsync();
        return projects.Count > 0;
    }
}
