using System.Linq;
using Kuestencode.Rapport.Models;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Rapport.Data.Repositories;

/// <summary>
/// Repository for accessing time entries.
/// </summary>
public class TimeEntryRepository : Repository<TimeEntry>
{
    public TimeEntryRepository(IDbContextFactory<RapportDbContext> contextFactory)
        : base(contextFactory)
    {
    }

    /// <summary>
    /// Returns the currently running entry for a specific user, if any.
    /// </summary>
    public async Task<TimeEntry?> GetRunningEntryAsync(Guid? teamMemberId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Set<TimeEntry>()
            .Where(e => e.Status == TimeEntryStatus.Running && e.EndTime == null);

        if (teamMemberId.HasValue)
            query = query.Where(e => e.TeamMemberId == teamMemberId.Value);

        return await query.FirstOrDefaultAsync();
    }

    /// <summary>
    /// Returns the currently running entry with customer data loaded for a specific user.
    /// </summary>
    public async Task<TimeEntry?> GetRunningEntryWithCustomerAsync(Guid? teamMemberId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Set<TimeEntry>()
            .Include(e => e.Customer)
            .Where(e => e.Status == TimeEntryStatus.Running && e.EndTime == null);

        if (teamMemberId.HasValue)
            query = query.Where(e => e.TeamMemberId == teamMemberId.Value);

        return await query.FirstOrDefaultAsync();
    }

    /// <summary>
    /// Returns entries within the given date range.
    /// </summary>
    public async Task<List<TimeEntry>> GetEntriesByDateRangeAsync(DateTime from, DateTime to)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Set<TimeEntry>()
            .Where(e => e.StartTime >= from && e.StartTime <= to)
            .OrderBy(e => e.StartTime)
            .ToListAsync();
    }

    /// <summary>
    /// Returns entries for a specific project.
    /// </summary>
    public async Task<List<TimeEntry>> GetEntriesByProjectIdAsync(int projectId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Set<TimeEntry>()
            .Where(e => e.ProjectId == projectId)
            .OrderByDescending(e => e.StartTime)
            .ToListAsync();
    }

    /// <summary>
    /// Returns manual entries.
    /// </summary>
    public async Task<List<TimeEntry>> GetManualEntriesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Set<TimeEntry>()
            .Where(e => e.IsManual || e.Status == TimeEntryStatus.Manual)
            .OrderByDescending(e => e.StartTime)
            .ToListAsync();
    }

    /// <summary>
    /// Calculates total hours for the given date range.
    /// </summary>
    public async Task<double> GetTotalHoursAsync(DateTime from, DateTime to)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        var entries = await context.Set<TimeEntry>()
            .Where(e => e.StartTime >= from && e.StartTime <= to)
            .ToListAsync();

        return entries
            .Select(e => (e.EndTime ?? now) - e.StartTime)
            .Sum(ts => ts.TotalHours);
    }

    /// <summary>
    /// Creates a new DbContext for query operations.
    /// The caller is responsible for disposing the context.
    /// </summary>
    public async Task<(RapportDbContext Context, IQueryable<TimeEntry> Query)> CreateQueryContextAsync()
    {
        var context = await _contextFactory.CreateDbContextAsync();
        return (context, context.Set<TimeEntry>().AsQueryable());
    }

    /// <summary>
    /// Returns entries that overlap the given time range for a specific user.
    /// </summary>
    public async Task<List<TimeEntry>> GetOverlappingEntriesAsync(DateTime start, DateTime end, int? excludeId = null, Guid? teamMemberId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Set<TimeEntry>().Where(e => e.StartTime < end && (e.EndTime ?? DateTime.UtcNow) > start);
        if (excludeId.HasValue)
        {
            query = query.Where(e => e.Id != excludeId.Value);
        }

        if (teamMemberId.HasValue)
        {
            query = query.Where(e => e.TeamMemberId == teamMemberId.Value);
        }

        return await query.ToListAsync();
    }
}
