using Kuestencode.Rapport.Models;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Rapport.Data.Repositories;

/// <summary>
/// Repository for accessing time entries.
/// </summary>
public class TimeEntryRepository : Repository<TimeEntry>
{
    public TimeEntryRepository(RapportDbContext context)
        : base(context)
    {
    }

    /// <summary>
    /// Returns the currently running entry, if any.
    /// </summary>
    public async Task<TimeEntry?> GetRunningEntryAsync()
    {
        return await _dbSet
            .Where(e => e.Status == TimeEntryStatus.Running && e.EndTime == null)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Returns the currently running entry with customer data loaded.
    /// </summary>
    public async Task<TimeEntry?> GetRunningEntryWithCustomerAsync()
    {
        return await _dbSet
            .Include(e => e.Customer)
            .Where(e => e.Status == TimeEntryStatus.Running && e.EndTime == null)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Returns entries within the given date range.
    /// </summary>
    public async Task<List<TimeEntry>> GetEntriesByDateRangeAsync(DateTime from, DateTime to)
    {
        return await _dbSet
            .Where(e => e.StartTime >= from && e.StartTime <= to)
            .OrderBy(e => e.StartTime)
            .ToListAsync();
    }

    /// <summary>
    /// Returns entries for a specific project.
    /// </summary>
    public async Task<List<TimeEntry>> GetEntriesByProjectIdAsync(int projectId)
    {
        return await _dbSet
            .Where(e => e.ProjectId == projectId)
            .OrderByDescending(e => e.StartTime)
            .ToListAsync();
    }

    /// <summary>
    /// Returns manual entries.
    /// </summary>
    public async Task<List<TimeEntry>> GetManualEntriesAsync()
    {
        return await _dbSet
            .Where(e => e.IsManual || e.Status == TimeEntryStatus.Manual)
            .OrderByDescending(e => e.StartTime)
            .ToListAsync();
    }

    /// <summary>
    /// Calculates total hours for the given date range.
    /// </summary>
    public async Task<double> GetTotalHoursAsync(DateTime from, DateTime to)
    {
        var now = DateTime.UtcNow;
        var entries = await _dbSet
            .Where(e => e.StartTime >= from && e.StartTime <= to)
            .ToListAsync();

        return entries
            .Select(e => (e.EndTime ?? now) - e.StartTime)
            .Sum(ts => ts.TotalHours);
    }
}
