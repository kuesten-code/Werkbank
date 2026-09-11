using Kuestencode.Core.Auth;
using Kuestencode.Werkbank.Acta.Data.Repositories;
using Kuestencode.Werkbank.Acta.Domain.Entities;
using Kuestencode.Werkbank.Acta.Domain.Enums;

namespace Kuestencode.Werkbank.Acta.Services;

public class PinnedProjectService : IPinnedProjectService
{
    private readonly IPinnedProjectRepository _pinnedRepo;
    private readonly IProjectRepository _projectRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public PinnedProjectService(
        IPinnedProjectRepository pinnedRepo,
        IProjectRepository projectRepo,
        ICurrentUserAccessor currentUser)
    {
        _pinnedRepo = pinnedRepo;
        _projectRepo = projectRepo;
        _currentUser = currentUser;
    }

    public async Task<List<Project>> GetPinnedProjectsAsync()
    {
        var userId = _currentUser.Get().UserId;
        var pins = await _pinnedRepo.GetByUserIdAsync(userId);
        return pins.Select(p => p.Project).ToList();
    }

    public async Task<List<Project>> GetPinnableActiveProjectsAsync()
    {
        var userId = _currentUser.Get().UserId;
        var activeProjects = await _projectRepo.GetByStatusAsync(ProjectStatus.Active);
        var pinnedIds = (await _pinnedRepo.GetByUserIdAsync(userId)).Select(p => p.ProjectId).ToHashSet();

        return activeProjects.Where(p => !pinnedIds.Contains(p.Id)).ToList();
    }

    public async Task PinAsync(Guid projectId)
    {
        var userId = _currentUser.Get().UserId;
        if (await _pinnedRepo.ExistsAsync(userId, projectId)) return;

        await _pinnedRepo.AddAsync(new PinnedProject
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProjectId = projectId,
            PinnedAt = DateTime.UtcNow
        });
    }

    public async Task UnpinAsync(Guid projectId)
    {
        var userId = _currentUser.Get().UserId;
        await _pinnedRepo.RemoveAsync(userId, projectId);
    }
}
