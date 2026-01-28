using System.ComponentModel.DataAnnotations;
using Kuestencode.Core.Interfaces;
using Kuestencode.Rapport.Data.Repositories;
using Kuestencode.Rapport.Models;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Rapport.Services;

/// <summary>
/// Service for manual time entry management.
/// </summary>
public class TimeEntryService
{
    private readonly TimeEntryRepository _timeEntryRepository;
    private readonly IProjectService _projectService;
    private readonly ICustomerService _customerService;
    private readonly SettingsService _settingsService;
    private readonly TimeRoundingService _roundingService;

    public TimeEntryService(
        TimeEntryRepository timeEntryRepository,
        IProjectService projectService,
        ICustomerService customerService,
        SettingsService settingsService,
        TimeRoundingService roundingService)
    {
        _timeEntryRepository = timeEntryRepository;
        _projectService = projectService;
        _customerService = customerService;
        _settingsService = settingsService;
        _roundingService = roundingService;
    }

    public async Task<TimeEntry> CreateManualEntryAsync(
        DateTime start,
        DateTime end,
        int? projectId,
        int? customerId,
        string? description)
    {
        ValidateManualEntryTimes(start, end);
        (start, end) = await ApplyManualRoundingAsync(start, end);

        var (resolvedCustomerId, resolvedCustomerName, resolvedProjectName) =
            await ResolveCustomerAndProjectAsync(projectId, customerId);

        await EnsureNoOverlapAsync(start, end, null);

        var entry = new TimeEntry
        {
            StartTime = start,
            EndTime = end,
            Description = description,
            IsManual = true,
            Status = TimeEntryStatus.Manual,
            CustomerId = resolvedCustomerId,
            CustomerName = resolvedCustomerName,
            ProjectId = projectId,
            ProjectName = resolvedProjectName
        };

        await _timeEntryRepository.AddAsync(entry);
        return entry;
    }

    public async Task<TimeEntry> UpdateEntryAsync(
        int id,
        DateTime start,
        DateTime end,
        int? projectId,
        int? customerId,
        string? description)
    {
        ValidateManualEntryTimes(start, end);
        (start, end) = await ApplyManualRoundingAsync(start, end);

        var entry = await _timeEntryRepository.GetByIdAsync(id);
        if (entry == null)
        {
            throw new KeyNotFoundException("Time entry not found.");
        }

        if (entry.Status == TimeEntryStatus.Running)
        {
            throw new ValidationException("Running entries cannot be edited.");
        }

        var (resolvedCustomerId, resolvedCustomerName, resolvedProjectName) =
            await ResolveCustomerAndProjectAsync(projectId, customerId);

        await EnsureNoOverlapAsync(start, end, id);

        entry.StartTime = start;
        entry.EndTime = end;
        entry.Description = description;
        entry.CustomerId = resolvedCustomerId;
        entry.CustomerName = resolvedCustomerName;
        entry.ProjectId = projectId;
        entry.ProjectName = resolvedProjectName;

        if (entry.IsManual)
        {
            entry.Status = TimeEntryStatus.Manual;
        }

        await _timeEntryRepository.UpdateAsync(entry);
        return entry;
    }

    public async Task SoftDeleteEntryAsync(int id)
    {
        var entry = await _timeEntryRepository.GetByIdAsync(id);
        if (entry == null)
        {
            throw new KeyNotFoundException("Time entry not found.");
        }

        entry.IsDeleted = true;
        entry.DeletedAt = DateTime.UtcNow;
        await _timeEntryRepository.UpdateAsync(entry);
    }

    public async Task<TimeEntry?> GetEntryAsync(int id)
    {
        return await _timeEntryRepository.GetByIdAsync(id);
    }

    public async Task<List<TimeEntry>> GetEntriesAsync(
        DateTime? from,
        DateTime? to,
        IEnumerable<int>? customerIds,
        IEnumerable<int>? projectIds,
        bool? manualOnly,
        bool? onlyWithoutProject = null)
    {
        IQueryable<TimeEntry> query = _timeEntryRepository.Query();

        DateTime? fromUtc = from.HasValue ? ToUtc(from.Value) : null;
        DateTime? toUtc = to.HasValue ? ToUtc(to.Value) : null;

        if (fromUtc.HasValue)
        {
            query = query.Where(e => e.StartTime >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(e => e.StartTime <= toUtc.Value);
        }

        if (customerIds != null && customerIds.Any())
        {
            query = query.Where(e => customerIds.Contains(e.CustomerId));
        }

        if (onlyWithoutProject == true)
        {
            query = query.Where(e => e.ProjectId == null);
        }
        else if (projectIds != null && projectIds.Any())
        {
            query = query.Where(e => e.ProjectId.HasValue && projectIds.Contains(e.ProjectId.Value));
        }

        if (manualOnly.HasValue)
        {
            if (manualOnly.Value)
            {
                query = query.Where(e => e.IsManual || e.Status == TimeEntryStatus.Manual);
            }
            else
            {
                query = query.Where(e => !e.IsManual && e.Status != TimeEntryStatus.Manual);
            }
        }

        return await query.OrderByDescending(e => e.StartTime).ToListAsync();
    }

    private static void ValidateManualEntryTimes(DateTime start, DateTime end)
    {
        if (start >= end)
        {
            throw new ValidationException("Start time must be before end time.");
        }

        var now = DateTime.UtcNow;
        if (start > now || end > now)
        {
            throw new ValidationException("Manual entries cannot be in the future.");
        }
    }

    private async Task<(int customerId, string? customerName, string? projectName)> ResolveCustomerAndProjectAsync(
        int? projectId,
        int? customerId)
    {
        if (projectId.HasValue)
        {
            return await ResolveProjectDetailsAsync(projectId.Value);
        }

        if (!customerId.HasValue)
        {
            throw new ValidationException("CustomerId is required when no project is selected.");
        }

        var directCustomer = await _customerService.GetByIdAsync(customerId.Value);
        if (directCustomer == null)
        {
            throw new ValidationException("Customer not found.");
        }

        return (directCustomer.Id, directCustomer.Name, null);
    }

    private async Task<(int customerId, string? customerName, string? projectName)> ResolveProjectDetailsAsync(int projectId)
    {
        var project = await _projectService.GetProjectByIdAsync(projectId);
        if (project == null)
        {
            throw new ValidationException("Selected project was not found.");
        }

        if (project.CustomerId <= 0)
        {
            throw new ValidationException("Selected project has no customer.");
        }

        var customer = await _customerService.GetByIdAsync(project.CustomerId);
        if (customer == null)
        {
            throw new ValidationException("Customer not found.");
        }

        return (project.CustomerId, project.CustomerName, project.Name);
    }


    private async Task<(DateTime Start, DateTime End)> ApplyManualRoundingAsync(DateTime start, DateTime end)
    {
        var settings = await _settingsService.GetSettingsAsync();
        if (settings.RoundingMinutes <= 0)
        {
            return (start, end);
        }

        var rounded = _roundingService.RoundDuration(end - start, settings.RoundingMinutes);
        var roundedEnd = start.Add(rounded);
        var now = DateTime.UtcNow;
        if (roundedEnd > now)
        {
            roundedEnd = now;
        }

        return (start, roundedEnd);
    }

    private static DateTime ToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime()
        };
    }

    private async Task EnsureNoOverlapAsync(DateTime start, DateTime end, int? excludeId)
    {
        var overlaps = await _timeEntryRepository.GetOverlappingEntriesAsync(start, end, excludeId);
        if (overlaps.Count > 0)
        {
            throw new ValidationException("Time entry overlaps with an existing entry.");
        }
    }
}

