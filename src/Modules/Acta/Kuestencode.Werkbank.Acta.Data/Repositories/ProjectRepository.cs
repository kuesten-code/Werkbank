using Kuestencode.Core.Services;
using Kuestencode.Shared.ApiClients;
using Kuestencode.Werkbank.Acta.Domain.Entities;
using Kuestencode.Werkbank.Acta.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Acta.Data.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly IDbContextFactory<ActaDbContext> _contextFactory;
    private readonly IHostApiClient _hostApiClient;

    public ProjectRepository(IDbContextFactory<ActaDbContext> contextFactory, IHostApiClient hostApiClient)
    {
        _contextFactory = contextFactory;
        _hostApiClient = hostApiClient;
    }

    public async Task<Project?> GetByIdAsync(Guid id)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.Projects
            .Include(p => p.Tasks.OrderBy(t => t.SortOrder))
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Project?> GetByNumberAsync(string projectNumber)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.Projects
            .Include(p => p.Tasks.OrderBy(t => t.SortOrder))
            .FirstOrDefaultAsync(p => p.ProjectNumber == projectNumber);
    }

    public async Task<List<Project>> GetAllAsync(ProjectStatus? status = null, int? customerId = null)
    {
        await using var context = _contextFactory.CreateDbContext();
        var query = context.Projects
            .Include(p => p.Tasks.OrderBy(t => t.SortOrder))
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (customerId.HasValue)
            query = query.Where(p => p.CustomerId == customerId.Value);

        return await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
    }

    public async Task<List<Project>> GetByCustomerAsync(int customerId)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.Projects
            .Include(p => p.Tasks.OrderBy(t => t.SortOrder))
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Project>> GetByStatusAsync(ProjectStatus status)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.Projects
            .Include(p => p.Tasks.OrderBy(t => t.SortOrder))
            .Where(p => p.Status == status)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(Project project)
    {
        await using var context = _contextFactory.CreateDbContext();
        await context.Projects.AddAsync(project);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Project project)
    {
        await using var context = _contextFactory.CreateDbContext();
        context.Projects.Update(project);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var context = _contextFactory.CreateDbContext();
        var project = await context.Projects.FindAsync(id);
        if (project == null)
            throw new InvalidOperationException($"Projekt mit ID {id} nicht gefunden.");

        if (project.Status != ProjectStatus.Draft)
            throw new InvalidOperationException(
                "Projekt kann nicht gelöscht werden. Nur Projekte im Status 'Draft' können gelöscht werden.");

        context.Projects.Remove(project);
        await context.SaveChangesAsync();
    }

    public async Task<bool> ExistsNumberAsync(string projectNumber)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.Projects.AnyAsync(p => p.ProjectNumber == projectNumber);
    }

    public async Task<string> GenerateProjectNumberAsync()
    {
        await using var context = _contextFactory.CreateDbContext();

        var settings = await _hostApiClient.GetNumberFormatSettingsAsync();
        var format = !string.IsNullOrWhiteSpace(settings?.ProjectFormat)
            ? settings.ProjectFormat.Trim()
            : "P-YYYY-XXXX";

        var existingNumbers = await context.Projects
            .Select(p => p.ProjectNumber)
            .ToListAsync();

        return DocumentNumberFormatter.GenerateNext(format, DateTime.Now, existingNumbers);
    }

    public async Task<int> GetNextExternalIdAsync()
    {
        await using var context = _contextFactory.CreateDbContext();
        var maxId = await context.Projects
            .Where(p => p.ExternalId.HasValue)
            .MaxAsync(p => (int?)p.ExternalId) ?? 0;
        return maxId + 1;
    }
}
