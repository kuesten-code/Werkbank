using Kuestencode.Werkbank.Acta.Domain.Entities;
using Kuestencode.Werkbank.Acta.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Acta.Data.Repositories;

/// <summary>
/// Repository-Implementierung für Projekte.
/// </summary>
public class ProjectRepository : IProjectRepository
{
    private readonly ActaDbContext _context;

    public ProjectRepository(ActaDbContext context)
    {
        _context = context;
    }

    public async Task<Project?> GetByIdAsync(Guid id)
    {
        return await _context.Projects
            .Include(p => p.Tasks.OrderBy(t => t.SortOrder))
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Project?> GetByNumberAsync(string projectNumber)
    {
        return await _context.Projects
            .Include(p => p.Tasks.OrderBy(t => t.SortOrder))
            .FirstOrDefaultAsync(p => p.ProjectNumber == projectNumber);
    }

    public async Task<List<Project>> GetAllAsync(ProjectStatus? status = null, int? customerId = null)
    {
        var query = _context.Projects
            .Include(p => p.Tasks.OrderBy(t => t.SortOrder))
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        if (customerId.HasValue)
        {
            query = query.Where(p => p.CustomerId == customerId.Value);
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Project>> GetByCustomerAsync(int customerId)
    {
        return await _context.Projects
            .Include(p => p.Tasks.OrderBy(t => t.SortOrder))
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Project>> GetByStatusAsync(ProjectStatus status)
    {
        return await _context.Projects
            .Include(p => p.Tasks.OrderBy(t => t.SortOrder))
            .Where(p => p.Status == status)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(Project project)
    {
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Project project)
    {
        _context.Projects.Update(project);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null)
        {
            throw new InvalidOperationException($"Projekt mit ID {id} nicht gefunden.");
        }

        if (project.Status != ProjectStatus.Draft)
        {
            throw new InvalidOperationException(
                "Projekt kann nicht gelöscht werden. Nur Projekte im Status 'Draft' können gelöscht werden.");
        }

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsNumberAsync(string projectNumber)
    {
        return await _context.Projects.AnyAsync(p => p.ProjectNumber == projectNumber);
    }
}
