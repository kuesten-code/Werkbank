using Kuestencode.Shared.Contracts.Acta;

namespace Kuestencode.Shared.ApiClients;

/// <summary>
/// API client interface for communication with the Acta module.
/// </summary>
public interface IActaApiClient
{
    Task<bool> CheckHealthAsync();
    Task<List<ActaProjectDto>> GetProjectsAsync();
    Task<ActaProjectDto?> GetProjectByExternalIdAsync(int externalId);
}
