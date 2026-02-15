using System.Text.Json;
using Kuestencode.Rapport.Data;
using Kuestencode.Rapport.Models;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Rapport.Services;

public class TimeEntryAuditService
{
    private readonly IDbContextFactory<RapportDbContext> _contextFactory;

    public TimeEntryAuditService(IDbContextFactory<RapportDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task LogCreateAsync(TimeEntry entry, Guid userId, string? userName)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.TimeEntryAudits.Add(new TimeEntryAudit
        {
            Id = Guid.NewGuid(),
            TimeEntryId = entry.Id,
            ChangedByUserId = userId,
            ChangedByUserName = userName,
            ChangedAt = DateTime.UtcNow,
            Action = "Created"
        });
        await context.SaveChangesAsync();
    }

    public async Task LogUpdateAsync(
        int timeEntryId,
        Dictionary<string, (object? Old, object? New)> changes,
        Guid userId,
        string? userName)
    {
        if (changes.Count == 0)
            return;

        var changesJson = JsonSerializer.Serialize(
            changes.ToDictionary(
                kv => kv.Key,
                kv => new { old = kv.Value.Old?.ToString(), @new = kv.Value.New?.ToString() }
            ));

        await using var context = await _contextFactory.CreateDbContextAsync();
        context.TimeEntryAudits.Add(new TimeEntryAudit
        {
            Id = Guid.NewGuid(),
            TimeEntryId = timeEntryId,
            ChangedByUserId = userId,
            ChangedByUserName = userName,
            ChangedAt = DateTime.UtcNow,
            Action = "Updated",
            Changes = changesJson
        });
        await context.SaveChangesAsync();
    }

    public async Task LogDeleteAsync(TimeEntry entry, Guid userId, string? userName)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.TimeEntryAudits.Add(new TimeEntryAudit
        {
            Id = Guid.NewGuid(),
            TimeEntryId = entry.Id,
            ChangedByUserId = userId,
            ChangedByUserName = userName,
            ChangedAt = DateTime.UtcNow,
            Action = "Deleted"
        });
        await context.SaveChangesAsync();
    }

    public async Task<List<TimeEntryAudit>> GetAuditsForEntryAsync(int timeEntryId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.TimeEntryAudits
            .Where(a => a.TimeEntryId == timeEntryId)
            .OrderByDescending(a => a.ChangedAt)
            .ToListAsync();
    }

    public async Task<TimeEntryAudit?> GetLatestUpdateAuditAsync(int timeEntryId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.TimeEntryAudits
            .Where(a => a.TimeEntryId == timeEntryId && a.Action == "Updated")
            .OrderByDescending(a => a.ChangedAt)
            .FirstOrDefaultAsync();
    }
}
