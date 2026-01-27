using System.ComponentModel.DataAnnotations;
using Kuestencode.Core.Interfaces;
using Kuestencode.Rapport.Data.Repositories;
using Kuestencode.Rapport.Models;
using Kuestencode.Rapport.Models.Timesheets;
using Kuestencode.Shared.Contracts.Rapport;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Rapport.Services;

/// <summary>
/// Builds timesheet data for exports (PDF/CSV).
/// </summary>
public class TimesheetExportService
{
    private readonly TimeEntryRepository _timeEntryRepository;
    private readonly ICustomerService _customerService;
    private readonly IProjectService _projectService;

    public TimesheetExportService(
        TimeEntryRepository timeEntryRepository,
        ICustomerService customerService,
        IProjectService projectService)
    {
        _timeEntryRepository = timeEntryRepository;
        _customerService = customerService;
        _projectService = projectService;
    }

    public async Task<TimesheetDto> BuildAsync(TimesheetExportRequestDto request)
    {
        if (request.CustomerId <= 0)
        {
            throw new ValidationException("CustomerId is required.");
        }

        if (request.From > request.To)
        {
            throw new ValidationException("From must be before To.");
        }

        var customer = await _customerService.GetByIdAsync(request.CustomerId);
        if (customer == null)
        {
            throw new ValidationException("Customer not found.");
        }

        int? projectId = request.ProjectId;
        string? projectName = null;

        if (projectId.HasValue)
        {
            var project = await _projectService.GetProjectByIdAsync(projectId.Value);
            if (project == null)
            {
                throw new ValidationException("Project not found.");
            }

            if (project.CustomerId != request.CustomerId)
            {
                throw new ValidationException("Project does not belong to selected customer.");
            }

            projectName = project.Name;
        }

        var entries = await LoadEntriesAsync(request, projectId);

        var dto = new TimesheetDto
        {
            Title = string.IsNullOrWhiteSpace(request.Title) ? "Tätigkeitsnachweis" : request.Title.Trim(),
            From = request.From,
            To = request.To,
            HourlyRate = request.HourlyRate,
            Customer = new TimesheetCustomerInfoDto
            {
                Name = customer.Name,
                CustomerNumber = customer.CustomerNumber,
                Address = customer.Address,
                PostalCode = customer.PostalCode,
                City = customer.City,
                Country = customer.Country
            },
            Project = projectId.HasValue
                ? new TimesheetProjectInfoDto { ProjectId = projectId.Value, ProjectName = projectName ?? "" }
                : null
        };

        var now = DateTime.UtcNow;

        foreach (var group in entries
            .GroupBy(e => e.ProjectId)
            .OrderBy(g => g.Key.HasValue ? 0 : 1)
            .ThenBy(g => g.Key))
        {
            var groupDto = new TimesheetProjectGroupDto
            {
                ProjectId = group.Key,
                ProjectName = group.Key.HasValue
                    ? group.FirstOrDefault()?.ProjectName ?? "Unbekanntes Projekt"
                    : "Ohne Projekt"
            };

            foreach (var entry in group.OrderBy(e => e.StartTime))
            {
                var duration = (entry.EndTime ?? now) - entry.StartTime;
                groupDto.Entries.Add(new TimesheetEntryDto
                {
                    Date = entry.StartTime.Date,
                    StartTime = entry.StartTime,
                    EndTime = entry.EndTime,
                    Description = string.IsNullOrWhiteSpace(entry.Description) ? "" : entry.Description.Trim(),
                    Duration = duration
                });

                groupDto.SubtotalHours += (decimal)duration.TotalHours;
                dto.TotalHours += (decimal)duration.TotalHours;
            }

            dto.Groups.Add(groupDto);
        }

        if (dto.HourlyRate.HasValue)
        {
            dto.TotalAmount = dto.TotalHours * dto.HourlyRate.Value;
        }

        return dto;
    }

    private async Task<List<TimeEntry>> LoadEntriesAsync(TimesheetExportRequestDto request, int? projectId)
    {
        IQueryable<TimeEntry> query = _timeEntryRepository.Query();

        query = query.Where(e => !e.IsDeleted);
        query = query.Where(e => e.CustomerId == request.CustomerId);

        if (request.EntryIds != null && request.EntryIds.Count > 0)
        {
            query = query.Where(e => request.EntryIds.Contains(e.Id));
        }
        else
        {
            var fromUtc = ToUtc(request.From);
            var toUtc = ToUtc(request.To);
            query = query.Where(e => e.StartTime >= fromUtc && e.StartTime <= toUtc);
        }

        if (projectId.HasValue)
        {
            query = query.Where(e => e.ProjectId == projectId.Value);
        }

        return await query
            .OrderBy(e => e.StartTime)
            .ToListAsync();
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
}

