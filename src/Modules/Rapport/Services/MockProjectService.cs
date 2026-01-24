using Kuestencode.Core.Interfaces;

namespace Kuestencode.Rapport.Services;

/// <summary>
/// Mock implementation of IProjectService for Rapport.
/// </summary>
public class MockProjectService : IProjectService
{
    public Task<List<IProject>> GetAllProjectsAsync()
    {
        return Task.FromResult(new List<IProject>());
    }

    public Task<IProject?> GetProjectByIdAsync(int id)
    {
        return Task.FromResult<IProject?>(null);
    }

    public Task<bool> IsAvailableAsync()
    {
        return Task.FromResult(false);
    }
}
