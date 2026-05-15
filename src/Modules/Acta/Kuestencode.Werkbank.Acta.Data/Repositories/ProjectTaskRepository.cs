using Kuestencode.Werkbank.Acta.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Acta.Data.Repositories;

public class ProjectTaskRepository : IProjectTaskRepository
{
    private readonly IDbContextFactory<ActaDbContext> _contextFactory;

    public ProjectTaskRepository(IDbContextFactory<ActaDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<ProjectTask?> GetByIdAsync(Guid id)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.Tasks
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<List<ProjectTask>> GetByProjectIdAsync(Guid projectId)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.Tasks
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.SortOrder)
            .ToListAsync();
    }

    public async Task<List<ProjectTask>> GetByAssignedUserIdAsync(Guid assignedUserId)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.Tasks
            .Include(t => t.Project)
            .Where(t => t.AssignedUserId == assignedUserId)
            .OrderBy(t => t.Status)
            .ThenBy(t => t.TargetDate)
            .ThenBy(t => t.SortOrder)
            .ToListAsync();
    }

    public async Task AddAsync(ProjectTask task)
    {
        await using var context = _contextFactory.CreateDbContext();
        await context.Tasks.AddAsync(task);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ProjectTask task)
    {
        await using var context = _contextFactory.CreateDbContext();
        context.Tasks.Update(task);
        await context.SaveChangesAsync();
    }

    public async Task UpdateRangeAsync(IEnumerable<ProjectTask> tasks)
    {
        await using var context = _contextFactory.CreateDbContext();
        context.Tasks.UpdateRange(tasks);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var context = _contextFactory.CreateDbContext();
        var task = await context.Tasks.FindAsync(id);
        if (task == null)
            throw new InvalidOperationException($"Aufgabe mit ID {id} nicht gefunden.");

        context.Tasks.Remove(task);
        await context.SaveChangesAsync();
    }

    public async Task<int> GetNextSortOrderAsync(Guid projectId)
    {
        await using var context = _contextFactory.CreateDbContext();
        var maxSortOrder = await context.Tasks
            .Where(t => t.ProjectId == projectId)
            .MaxAsync(t => (int?)t.SortOrder) ?? 0;

        return maxSortOrder + 1;
    }
}
