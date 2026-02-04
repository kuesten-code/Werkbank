using Kuestencode.Werkbank.Acta.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Acta.Data.Repositories;

/// <summary>
/// Repository-Implementierung für Projektaufgaben.
/// </summary>
public class ProjectTaskRepository : IProjectTaskRepository
{
    private readonly ActaDbContext _context;

    public ProjectTaskRepository(ActaDbContext context)
    {
        _context = context;
    }

    public async Task<ProjectTask?> GetByIdAsync(Guid id)
    {
        return await _context.Tasks
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<List<ProjectTask>> GetByProjectIdAsync(Guid projectId)
    {
        return await _context.Tasks
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.SortOrder)
            .ToListAsync();
    }

    public async Task AddAsync(ProjectTask task)
    {
        await _context.Tasks.AddAsync(task);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ProjectTask task)
    {
        _context.Tasks.Update(task);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateRangeAsync(IEnumerable<ProjectTask> tasks)
    {
        _context.Tasks.UpdateRange(tasks);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null)
        {
            throw new InvalidOperationException($"Aufgabe mit ID {id} nicht gefunden.");
        }

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();
    }

    public async Task<int> GetNextSortOrderAsync(Guid projectId)
    {
        var maxSortOrder = await _context.Tasks
            .Where(t => t.ProjectId == projectId)
            .MaxAsync(t => (int?)t.SortOrder) ?? 0;

        return maxSortOrder + 1;
    }
}
