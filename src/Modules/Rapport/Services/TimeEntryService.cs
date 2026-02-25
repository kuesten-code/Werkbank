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
    private readonly IUserContextService _userContextService;
    private readonly TimeEntryAuditService _auditService;

    private static readonly TimeSpan EditWindow = TimeSpan.FromDays(14);

    public TimeEntryService(
        TimeEntryRepository timeEntryRepository,
        IProjectService projectService,
        ICustomerService customerService,
        SettingsService settingsService,
        TimeRoundingService roundingService,
        IUserContextService userContextService,
        TimeEntryAuditService auditService)
    {
        _timeEntryRepository = timeEntryRepository;
        _projectService = projectService;
        _customerService = customerService;
        _settingsService = settingsService;
        _roundingService = roundingService;
        _userContextService = userContextService;
        _auditService = auditService;
    }

    public async Task<TimeEntry> CreateManualEntryAsync(
        DateTime start,
        DateTime end,
        int? projectId,
        int? customerId,
        string? description,
        Guid? teamMemberId = null,
        string? teamMemberName = null)
    {
        ValidateManualEntryTimes(start, end);
        (start, end) = await ApplyManualRoundingAsync(start, end);

        var (resolvedCustomerId, resolvedCustomerName, resolvedProjectName) =
            await ResolveCustomerAndProjectAsync(projectId, customerId);

        await EnsureNoOverlapAsync(start, end, null, teamMemberId);

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
            ProjectName = resolvedProjectName,
            TeamMemberId = teamMemberId,
            TeamMemberName = teamMemberName
        };

        await _timeEntryRepository.AddAsync(entry);

        if (teamMemberId.HasValue)
        {
            await _auditService.LogCreateAsync(entry, teamMemberId.Value, teamMemberName);
        }

        return entry;
    }

    public async Task<TimeEntry> UpdateEntryAsync(
        int id,
        DateTime start,
        DateTime end,
        int? projectId,
        int? customerId,
        string? description,
        Guid? teamMemberId = null,
        string? teamMemberName = null)
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

        await EnsureCanEditAsync(entry);

        var (resolvedCustomerId, resolvedCustomerName, resolvedProjectName) =
            await ResolveCustomerAndProjectAsync(projectId, customerId);

        await EnsureNoOverlapAsync(start, end, id, entry.TeamMemberId);

        // Track changes for audit
        var changes = new Dictionary<string, (object? Old, object? New)>();
        if (entry.StartTime != start) changes["StartTime"] = (entry.StartTime, start);
        if (entry.EndTime != end) changes["EndTime"] = (entry.EndTime, end);
        if (entry.CustomerId != resolvedCustomerId) changes["CustomerId"] = (entry.CustomerId, resolvedCustomerId);
        if (entry.CustomerName != resolvedCustomerName) changes["CustomerName"] = (entry.CustomerName, resolvedCustomerName);
        if (entry.ProjectId != projectId) changes["ProjectId"] = (entry.ProjectId, projectId);
        if (entry.ProjectName != resolvedProjectName) changes["ProjectName"] = (entry.ProjectName, resolvedProjectName);
        if (entry.Description != description) changes["Description"] = (entry.Description, description);

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

        var currentUserId = await _userContextService.GetCurrentUserIdAsync();
        var currentUserName = await _userContextService.GetCurrentUserNameAsync();
        if (currentUserId.HasValue && changes.Count > 0)
        {
            await _auditService.LogUpdateAsync(id, changes, currentUserId.Value, currentUserName);
        }

        return entry;
    }

    public async Task SoftDeleteEntryAsync(int id)
    {
        var entry = await _timeEntryRepository.GetByIdAsync(id);
        if (entry == null)
        {
            throw new KeyNotFoundException("Time entry not found.");
        }

        await EnsureCanEditAsync(entry);

        entry.IsDeleted = true;
        entry.DeletedAt = DateTime.UtcNow;
        await _timeEntryRepository.UpdateAsync(entry);

        var currentUserId = await _userContextService.GetCurrentUserIdAsync();
        var currentUserName = await _userContextService.GetCurrentUserNameAsync();
        if (currentUserId.HasValue)
        {
            await _auditService.LogDeleteAsync(entry, currentUserId.Value, currentUserName);
        }
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
        bool? onlyWithoutProject = null,
        Guid? teamMemberId = null)
    {
        var (context, query) = await _timeEntryRepository.CreateQueryContextAsync();
        await using (context)
        {
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

            if (teamMemberId.HasValue)
            {
                query = query.Where(e => e.TeamMemberId == teamMemberId.Value);
            }

            return await query.OrderByDescending(e => e.StartTime).ToListAsync();
        }
    }

    /// <summary>
    /// Checks if the current user can edit the given entry.
    /// Admin/Büro can always edit. Mitarbeiter can edit own entries within 14 days.
    /// </summary>
    public async Task<bool> CanEditEntryAsync(TimeEntry entry)
    {
        var isAdminOrBuero = await _userContextService.IsAdminOrBueroAsync();
        if (isAdminOrBuero)
            return true;

        var currentUserId = await _userContextService.GetCurrentUserIdAsync();
        if (currentUserId == null || entry.TeamMemberId != currentUserId)
            return false;

        return IsWithinEditWindow(entry);
    }

    private async Task EnsureCanEditAsync(TimeEntry entry)
    {
        if (!await CanEditEntryAsync(entry))
        {
            throw new ValidationException("Sie haben keine Berechtigung, diesen Eintrag zu bearbeiten.");
        }
    }

    private static bool IsWithinEditWindow(TimeEntry entry)
    {
        return (DateTime.UtcNow - entry.StartTime) < EditWindow;
    }

    private static void ValidateManualEntryTimes(DateTime start, DateTime end)
    {
        if (start >= end)
        {
            throw new ValidationException("Start time must be before end time.");
        }

        var now = DateTime.UtcNow.AddMinutes(1);
        if (start > now)
        {
            throw new ValidationException("Start time cannot be in the future.");
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

    private async Task EnsureNoOverlapAsync(DateTime start, DateTime end, int? excludeId, Guid? teamMemberId)
    {
        var overlaps = await _timeEntryRepository.GetOverlappingEntriesAsync(start, end, excludeId, teamMemberId);
        if (overlaps.Count > 0)
        {
            throw new ValidationException("Time entry overlaps with an existing entry.");
        }
    }
}
