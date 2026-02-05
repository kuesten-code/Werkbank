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

    public async Task<string> GenerateProjectNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"P-{year}-";

        var lastNumber = await _context.Projects
            .Where(p => p.ProjectNumber.StartsWith(prefix))
            .OrderByDescending(p => p.ProjectNumber)
            .Select(p => p.ProjectNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastNumber != null)
        {
            var numberPart = lastNumber[prefix.Length..];
            if (int.TryParse(numberPart, out var num))
                nextNumber = num + 1;
        }

        return $"{prefix}{nextNumber:D4}";
    }

    public async Task<int> GetNextExternalIdAsync()
    {
        var maxId = await _context.Projects
            .Where(p => p.ExternalId.HasValue)
            .MaxAsync(p => (int?)p.ExternalId) ?? 0;
        return maxId + 1;
    }
}
