using Kuestencode.Core.Interfaces;
using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.Contracts.Acta;
using Kuestencode.Shared.Contracts.Navigation;

namespace Kuestencode.Rapport.Services;

/// <summary>
/// IProjectService-Implementierung die Projekte über den Host von Acta bezieht.
/// Prüft dynamisch ob Acta verfügbar ist - wenn nicht, verhält sich wie MockProjectService.
/// </summary>
public class ApiProjectService : IProjectService
{
    private readonly IHostApiClient _hostApiClient;

    public ApiProjectService(IHostApiClient hostApiClient)
    {
        _hostApiClient = hostApiClient;
    }

    public async Task<List<IProject>> GetAllProjectsAsync()
    {
        if (!await IsAvailableAsync())
            return [];

        var dtos = await _hostApiClient.GetActaProjectsAsync();
        return dtos.Select(d => (IProject)new ActaProject(d)).ToList();
    }

    public async Task<IProject?> GetProjectByIdAsync(int id)
    {
        if (!await IsAvailableAsync())
            return null;

        var dto = await _hostApiClient.GetActaProjectAsync(id);
        return dto != null ? new ActaProject(dto) : null;
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var navItems = await _hostApiClient.GetNavigationAsync();
            return navItems.Any(IsActaNavItem);
        }
        catch
        {
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

    private class ActaProject : IProject
    {
        public ActaProject(ActaProjectDto dto)
        {
            Id = dto.Id;
            Name = dto.Name;
            ProjectNumber = dto.ProjectNumber;
            CustomerId = dto.CustomerId;
            CustomerName = dto.CustomerName;
        }

        public int Id { get; }
        public string Name { get; }
        public string? ProjectNumber { get; }
        public int CustomerId { get; }
        public string CustomerName { get; }
    }
}
