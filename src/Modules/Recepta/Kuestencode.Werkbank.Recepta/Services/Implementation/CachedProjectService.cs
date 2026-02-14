using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.Contracts.Acta;
using Kuestencode.Shared.Contracts.Navigation;
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

        try
        {
            var projects = await _hostApiClient.GetActaProjectsAsync();
            _cache.Set(ProjectsCacheKey, projects, CacheDuration);
            _cache.Set(AvailabilityCacheKey, projects.Count > 0, CacheDuration);
            return projects;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Acta-Projekte konnten nicht geladen werden");
            _cache.Set(AvailabilityCacheKey, false, CacheDuration);
            return [];
        }
    }

    public async Task<bool> IsActaAvailableAsync()
    {
        if (_cache.TryGetValue(AvailabilityCacheKey, out bool available))
        {
            return available;
        }

        try
        {
            var navItems = await _hostApiClient.GetNavigationAsync();
            var isAvailable = navItems.Any(IsActaNavItem);
            _cache.Set(AvailabilityCacheKey, isAvailable, CacheDuration);
            return isAvailable;
        }
        catch
        {
            _cache.Set(AvailabilityCacheKey, false, CacheDuration);
            return false;
        }
    }

    private static bool IsActaNavItem(NavItemDto item)
    {
        if (!string.IsNullOrWhiteSpace(item.Href) && item.Href.StartsWith("/acta", StringComparison.OrdinalIgnoreCase))
            return true;

        if (item.Children is { Count: > 0 })
            return item.Children.Any(IsActaNavItem);

        return false;
    }
}
