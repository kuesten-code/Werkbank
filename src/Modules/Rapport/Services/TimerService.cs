using System.ComponentModel.DataAnnotations;
using Kuestencode.Core.Interfaces;
using Kuestencode.Rapport.Data.Repositories;
using Kuestencode.Rapport.Models;

namespace Kuestencode.Rapport.Services;

/// <summary>
/// Service for starting and stopping time tracking.
/// Per-user timers: each user can have one running timer.
/// </summary>
public class TimerService
{
    private readonly TimeEntryRepository _timeEntryRepository;
    private readonly IProjectService _projectService;
    private readonly ICustomerService _customerService;
    private readonly SettingsService _settingsService;
    private readonly TimeRoundingService _roundingService;

    public TimerService(
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

    /// <summary>
    /// Starts a new timer for the given user.
    /// </summary>
    public async Task<TimeEntry> StartTimerAsync(
        int? projectId,
        int? customerId,
        string? description = null,
        Guid? teamMemberId = null,
        string? teamMemberName = null)
    {
        var existing = await _timeEntryRepository.GetRunningEntryAsync(teamMemberId);
        if (existing != null)
        {
            throw new ValidationException("A timer is already running.");
        }

        int resolvedCustomerId;
        string? resolvedCustomerName;
        string? resolvedProjectName = null;

        if (projectId.HasValue)
        {
            (resolvedCustomerId, resolvedCustomerName, resolvedProjectName) =
                await ResolveProjectDetailsAsync(projectId.Value);
        }
        else
        {
            if (!customerId.HasValue)
            {
                throw new ValidationException("CustomerId is required when no project is selected.");
            }

            resolvedCustomerId = customerId.Value;
            var customer = await _customerService.GetByIdAsync(resolvedCustomerId);
            if (customer == null)
            {
                throw new ValidationException("Customer not found.");
            }

            resolvedCustomerName = customer.Name;
        }

        var now = DateTime.UtcNow;

        var entry = new TimeEntry
        {
            StartTime = now,
            EndTime = null,
            Description = description,
            IsManual = false,
            CustomerId = resolvedCustomerId,
            CustomerName = resolvedCustomerName,
            ProjectId = projectId,
            ProjectName = resolvedProjectName,
            Status = TimeEntryStatus.Running,
            TeamMemberId = teamMemberId,
            TeamMemberName = teamMemberName
        };

        await _timeEntryRepository.AddAsync(entry);
        return entry;
    }

    /// <summary>
    /// Stops a running timer.
    /// </summary>
    public async Task<TimeEntry> StopTimerAsync(int timeEntryId, string? finalDescription = null)
    {
        var entry = await _timeEntryRepository.GetByIdAsync(timeEntryId);
        if (entry == null)
        {
            throw new KeyNotFoundException("Time entry not found.");
        }

        if (entry.EndTime != null)
        {
            throw new ValidationException("Timer is already stopped.");
        }

        var now = DateTime.UtcNow;
        if (entry.StartTime > now)
        {
            throw new ValidationException("Start time cannot be in the future.");
        }

        entry.EndTime = now;
        entry.Status = TimeEntryStatus.Stopped;

        var settings = await _settingsService.GetSettingsAsync();
        if (settings.RoundingMinutes > 0)
        {
            var rounded = _roundingService.RoundDuration(now - entry.StartTime, settings.RoundingMinutes);
            entry.EndTime = entry.StartTime.Add(rounded);
        }

        if (!string.IsNullOrWhiteSpace(finalDescription))
        {
            entry.Description = finalDescription;
        }

        await _timeEntryRepository.UpdateAsync(entry);
        return entry;
    }

    /// <summary>
    /// Returns the currently running timer for a specific user.
    /// </summary>
    public async Task<TimeEntry?> GetRunningTimerAsync(Guid? teamMemberId = null)
    {
        return await _timeEntryRepository.GetRunningEntryWithCustomerAsync(teamMemberId);
    }

    /// <summary>
    /// Returns the current duration of the running timer for a specific user.
    /// </summary>
    public async Task<TimeSpan> GetCurrentDurationAsync(Guid? teamMemberId = null)
    {
        var entry = await _timeEntryRepository.GetRunningEntryAsync(teamMemberId);
        return entry == null ? TimeSpan.Zero : entry.Duration;
    }

    /// <summary>
    /// Checks if a timer is currently running for a specific user.
    /// </summary>
    public async Task<bool> IsTimerRunningAsync(Guid? teamMemberId = null)
    {
        var entry = await _timeEntryRepository.GetRunningEntryAsync(teamMemberId);
        return entry != null;
    }

    private async Task<(int customerId, string customerName, string projectName)> ResolveProjectDetailsAsync(int projectId)
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
}
