using Kuestencode.Rapport.Data.Repositories;
using Kuestencode.Rapport.Models;
using Kuestencode.Rapport.Models.Reports;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Rapport.Services;

/// <summary>
/// Service for aggregated time entry reporting.
/// </summary>
public class DashboardService
{
    private readonly TimeEntryRepository _timeEntryRepository;

    public DashboardService(TimeEntryRepository timeEntryRepository)
    {
        _timeEntryRepository = timeEntryRepository;
    }

    /// <summary>
    /// Returns aggregated hours by customer.
    /// </summary>
    public async Task<Dictionary<int, CustomerHoursDto>> GetHoursByCustomerAsync(DateTime from, DateTime to)
    {
        var entries = await GetEntriesAsync(from, to, null, null);
        return BuildCustomerAggregation(entries);
    }

    /// <summary>
    /// Returns aggregated hours for a single customer with project breakdown.
    /// </summary>
    public async Task<CustomerHoursDto?> GetHoursByCustomerAndProjectAsync(int customerId, DateTime from, DateTime to)
    {
        var entries = await GetEntriesAsync(from, to, new[] { customerId }, null);
        var aggregation = BuildCustomerAggregation(entries);
        return aggregation.TryGetValue(customerId, out var dto) ? dto : null;
    }

    /// <summary>
    /// Returns the top customers by hours within a date range.
    /// </summary>
    public async Task<List<CustomerHoursDto>> GetTopCustomersAsync(DateTime from, DateTime to, int limit = 5)
    {
        var aggregation = await GetHoursByCustomerAsync(from, to);
        return aggregation.Values
            .OrderByDescending(c => c.TotalHours)
            .Take(limit)
            .ToList();
    }

    /// <summary>
    /// Returns time entries within a date range with optional filters.
    /// </summary>
    public async Task<List<TimeEntry>> GetEntriesAsync(
        DateTime from,
        DateTime to,
        IEnumerable<int>? customerIds,
        IEnumerable<int>? projectIds)
    {
        IQueryable<TimeEntry> query = _timeEntryRepository.Query();

        var fromUtc = ToUtc(from);
        var toUtc = ToUtc(to);

        query = query.Where(e => e.StartTime >= fromUtc && e.StartTime <= toUtc);

        if (customerIds != null && customerIds.Any())
        {
            query = query.Where(e => customerIds.Contains(e.CustomerId));
        }

        if (projectIds != null && projectIds.Any())
        {
            query = query.Where(e => e.ProjectId.HasValue && projectIds.Contains(e.ProjectId.Value));
        }

        return await query.OrderBy(e => e.StartTime).ToListAsync();
    }

    private static Dictionary<int, CustomerHoursDto> BuildCustomerAggregation(List<TimeEntry> entries)
    {
        var now = DateTime.UtcNow;
        var result = new Dictionary<int, CustomerHoursDto>();

        foreach (var entry in entries)
        {
            if (!result.TryGetValue(entry.CustomerId, out var dto))
            {
                dto = new CustomerHoursDto
                {
                    CustomerId = entry.CustomerId,
                    CustomerName = entry.CustomerName ?? "Unbekannter Kunde"
                };
                result[entry.CustomerId] = dto;
            }

            var duration = (entry.EndTime ?? now) - entry.StartTime;
            var hours = (decimal)duration.TotalHours;
            dto.TotalHours += hours;

            if (entry.ProjectId.HasValue)
            {
                var project = dto.Projects.FirstOrDefault(p => p.ProjectId == entry.ProjectId.Value);
                if (project == null)
                {
                    project = new ProjectHoursDto
                    {
                        ProjectId = entry.ProjectId.Value,
                        ProjectName = entry.ProjectName ?? "Unbekanntes Projekt"
                    };
                    dto.Projects.Add(project);
                }

                project.Hours += hours;
            }
            else
            {
                dto.EntriesWithoutProject += hours;
            }
        }

        foreach (var dto in result.Values)
        {
            dto.Projects = dto.Projects
                .OrderByDescending(p => p.Hours)
                .ToList();
        }

        return result;
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



