using System.ComponentModel.DataAnnotations;
using Kuestencode.Core.Interfaces;
using Kuestencode.Rapport.Data.Repositories;
using Kuestencode.Rapport.Models;

namespace Kuestencode.Rapport.Services;

/// <summary>
/// Service for starting and stopping time tracking.
/// </summary>
public class TimerService
{
    private static readonly SemaphoreSlim _lock = new(1, 1);
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
    /// Starts a new timer.
    /// </summary>
    public async Task<TimeEntry> StartTimerAsync(int? projectId, int? customerId, string? description = null)
    {
        await _lock.WaitAsync();
        try
        {
            var existing = await _timeEntryRepository.GetRunningEntryAsync();
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
                Status = TimeEntryStatus.Running
            };

            await _timeEntryRepository.AddAsync(entry);
            return entry;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Stops a running timer.
    /// </summary>
    public async Task<TimeEntry> StopTimerAsync(int timeEntryId, string? finalDescription = null)
    {
        await _lock.WaitAsync();
        try
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
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Returns the currently running timer with customer data loaded.
    /// </summary>
    public async Task<TimeEntry?> GetRunningTimerAsync()
    {
        await _lock.WaitAsync();
        try
        {
            return await _timeEntryRepository.GetRunningEntryWithCustomerAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Returns the current duration of the running timer.
    /// </summary>
    public async Task<TimeSpan> GetCurrentDurationAsync()
    {
        var entry = await _timeEntryRepository.GetRunningEntryAsync();
        return entry == null ? TimeSpan.Zero : entry.Duration;
    }

    /// <summary>
    /// Checks if a timer is currently running.
    /// </summary>
    public async Task<bool> IsTimerRunningAsync()
    {
        var entry = await _timeEntryRepository.GetRunningEntryAsync();
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

