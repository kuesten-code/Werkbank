namespace Kuestencode.Core.Interfaces;

/// <summary>
/// Provides access to projects from a host system.
/// </summary>
public interface IProjectService
{
    Task<List<IProject>> GetAllProjectsAsync();
    Task<IProject?> GetProjectByIdAsync(int id);
    Task<bool> IsAvailableAsync();
}
