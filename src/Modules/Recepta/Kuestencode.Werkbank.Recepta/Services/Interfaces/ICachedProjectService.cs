using Kuestencode.Shared.Contracts.Acta;

namespace Kuestencode.Werkbank.Recepta.Services;

/// <summary>
/// Cached service for loading Acta projects.
/// Caches the project list for 5 minutes to reduce cross-module API calls.
/// </summary>
public interface ICachedProjectService
{
    Task<List<ActaProjectDto>> GetProjectsAsync();
    Task<bool> IsActaAvailableAsync();
}
