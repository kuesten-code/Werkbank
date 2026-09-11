using Kuestencode.Werkbank.Acta.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Acta.Data.Repositories;

public class PinnedProjectRepository : IPinnedProjectRepository
{
    private readonly IDbContextFactory<ActaDbContext> _contextFactory;

    public PinnedProjectRepository(IDbContextFactory<ActaDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<PinnedProject>> GetByUserIdAsync(Guid userId)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.PinnedProjects
            .Include(p => p.Project)
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.PinnedAt)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(Guid userId, Guid projectId)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.PinnedProjects
            .AnyAsync(p => p.UserId == userId && p.ProjectId == projectId);
    }

    public async Task AddAsync(PinnedProject pin)
    {
        await using var context = _contextFactory.CreateDbContext();
        context.PinnedProjects.Add(pin);
        await context.SaveChangesAsync();
    }

    public async Task<bool> RemoveAsync(Guid userId, Guid projectId)
    {
        await using var context = _contextFactory.CreateDbContext();
        var pin = await context.PinnedProjects
            .FirstOrDefaultAsync(p => p.UserId == userId && p.ProjectId == projectId);
        if (pin == null) return false;

        context.PinnedProjects.Remove(pin);
        await context.SaveChangesAsync();
        return true;
    }
}
