using Kuestencode.Core.Interfaces;

namespace Kuestencode.Rapport.IntegrationTests.TestDoubles;

public sealed class TestProjectService : IProjectService
{
    private readonly List<IProject> _projects = new();

    public IReadOnlyList<IProject> Projects => _projects;

    public Task<List<IProject>> GetAllProjectsAsync()
    {
        return Task.FromResult(_projects.ToList());
    }

    public Task<IProject?> GetProjectByIdAsync(int id)
    {
        return Task.FromResult(_projects.FirstOrDefault(p => p.Id == id));
    }

    public Task<bool> IsAvailableAsync()
    {
        return Task.FromResult(true);
    }

    public void AddProject(IProject project)
    {
        _projects.Add(project);
    }

    public sealed class TestProject : IProject
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? ProjectNumber { get; init; }
        public int CustomerId { get; init; }
        public string CustomerName { get; init; } = string.Empty;
    }
}
