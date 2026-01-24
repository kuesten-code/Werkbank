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

    public TimerService(
        TimeEntryRepository timeEntryRepository,
        IProjectService projectService,
        ICustomerService customerService)
    {
        _timeEntryRepository = timeEntryRepository;
        _projectService = projectService;
        _customerService = customerService;
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
                var project = await _projectService.GetProjectByIdAsync(projectId.Value);
                if (project == null)
                {
                    throw new ValidationException("Selected project was not found.");
                }

                resolvedCustomerId = project.CustomerId;
                resolvedProjectName = project.Name;

                var customer = await _customerService.GetByIdAsync(resolvedCustomerId);
                if (customer == null)
                {
                    throw new ValidationException("Customer not found.");
                }

                resolvedCustomerName = project.CustomerName;
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
    public Task<TimeEntry?> GetRunningTimerAsync()
    {
        return _timeEntryRepository.GetRunningEntryWithCustomerAsync();
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
}
