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

    public TimeEntryService(
        TimeEntryRepository timeEntryRepository,
        IProjectService projectService,
        ICustomerService customerService)
    {
        _timeEntryRepository = timeEntryRepository;
        _projectService = projectService;
        _customerService = customerService;
    }

    public async Task<TimeEntry> CreateManualEntryAsync(
        DateTime start,
        DateTime end,
        int? projectId,
        int? customerId,
        string? description)
    {
        ValidateManualEntryTimes(start, end);

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
        bool? manualOnly)
    {
        IQueryable<TimeEntry> query = _timeEntryRepository.Query();

        if (from.HasValue)
        {
            query = query.Where(e => e.StartTime >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(e => e.StartTime <= to.Value);
        }

        if (customerIds != null && customerIds.Any())
        {
            query = query.Where(e => customerIds.Contains(e.CustomerId));
        }

        if (projectIds != null && projectIds.Any())
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
            var project = await _projectService.GetProjectByIdAsync(projectId.Value);
            if (project == null)
            {
                throw new ValidationException("Selected project was not found.");
            }

            var customer = await _customerService.GetByIdAsync(project.CustomerId);
            if (customer == null)
            {
                throw new ValidationException("Customer not found.");
            }

            return (project.CustomerId, project.CustomerName, project.Name);
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

    private async Task EnsureNoOverlapAsync(DateTime start, DateTime end, int? excludeId)
    {
        var overlaps = await _timeEntryRepository.GetOverlappingEntriesAsync(start, end, excludeId);
        if (overlaps.Count > 0)
        {
            throw new ValidationException("Time entry overlaps with an existing entry.");
        }
    }
}



