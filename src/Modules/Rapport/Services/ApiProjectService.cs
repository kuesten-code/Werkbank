using Kuestencode.Core.Interfaces;
using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.Contracts.Acta;

namespace Kuestencode.Rapport.Services;

public class ApiProjectService : IProjectService
{
    private readonly IHostApiClient _hostApiClient;
    private readonly ModuleAvailabilityService _availability;

    public ApiProjectService(IHostApiClient hostApiClient, ModuleAvailabilityService availability)
    {
        _hostApiClient = hostApiClient;
        _availability = availability;
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
        return (await _availability.CheckAsync()).Acta;
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
