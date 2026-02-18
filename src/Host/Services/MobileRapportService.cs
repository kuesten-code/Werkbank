using Kuestencode.Core.Interfaces;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Models.MobileRapport;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;

namespace Kuestencode.Werkbank.Host.Services;

public class MobileRapportService : IMobileRapportService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICustomerService _customerService;
    private readonly HostDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MobileRapportService> _logger;

    public MobileRapportService(
        IHttpClientFactory httpClientFactory,
        ICustomerService customerService,
        HostDbContext dbContext,
        IConfiguration configuration,
        ILogger<MobileRapportService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _customerService = customerService;
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<List<ProjectSelectDto>> GetProjectsAsync(Guid teamMemberId)
    {
        // Erstmal Kunden als Projekte verwenden
        // TODO: Später auf Acta/Project-Integration erweitern
        var customers = await _customerService.GetAllAsync();

        return customers
            .OrderBy(c => c.Name)
            .Select(c => new ProjectSelectDto
            {
                Id = c.Id,
                Name = c.Name,
                ProjectNumber = c.CustomerNumber
            })
            .ToList();
    }

    public async Task<List<TimeEntryDto>> GetEntriesAsync(Guid teamMemberId, DateOnly date)
    {
        return await GetEntriesAsync(teamMemberId, date, date);
    }

    public async Task<List<TimeEntryDto>> GetEntriesAsync(Guid teamMemberId, DateOnly from, DateOnly to)
    {
        try
        {
            var client = CreateRapportClient();

            var fromDateTime = from.ToDateTime(TimeOnly.MinValue);
            var toDateTime = to.ToDateTime(TimeOnly.MaxValue);

            var response = await client.GetAsync(
                $"/api/rapport/entries?from={fromDateTime:yyyy-MM-dd}&to={toDateTime:yyyy-MM-dd}&teamMemberId={teamMemberId}");

            if (response.IsSuccessStatusCode)
            {
                var entries = await response.Content.ReadFromJsonAsync<List<RapportTimeEntry>>();
                if (entries == null) return new List<TimeEntryDto>();

                var now = DateTime.UtcNow;
                var maxEditDate = DateOnly.FromDateTime(now.AddDays(-14));

                return entries
                    .Where(e => !e.IsDeleted)
                    .Select(e =>
                    {
                        var entryDate = DateOnly.FromDateTime(e.StartTime);
                        return new TimeEntryDto
                        {
                            Id = e.Id,
                            CustomerId = e.CustomerId,
                            ProjectName = e.ProjectName ?? e.CustomerName ?? "Unbekannt",
                            Date = entryDate,
                            Hours = (decimal)(e.Duration?.TotalHours ?? 0),
                            Description = e.Description,
                            CanEdit = entryDate >= maxEditDate,
                            CanDelete = entryDate >= maxEditDate
                        };
                    })
                    .OrderByDescending(e => e.Date)
                    .ToList();
            }

            return new List<TimeEntryDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading time entries for team member {TeamMemberId}", teamMemberId);
            throw new InvalidOperationException("Fehler beim Laden der Zeiteinträge", ex);
        }
    }

    public async Task<TimeEntryDto> CreateEntryAsync(Guid teamMemberId, CreateTimeEntryDto dto)
    {
        try
        {
            var client = CreateRapportClient();

            var startTime = dto.Date.ToDateTime(new TimeOnly(8, 0));
            var endTime = startTime.AddHours((double)dto.Hours);

            var rapportEntry = new
            {
                startTime,
                endTime,
                description = dto.Description,
                isManual = true,
                customerId = dto.CustomerId,
                status = "Completed",
                teamMemberId
            };

            var response = await client.PostAsJsonAsync("/api/rapport/entries", rapportEntry);

            if (response.IsSuccessStatusCode)
            {
                var created = await response.Content.ReadFromJsonAsync<RapportTimeEntry>();
                if (created != null)
                {
                    return new TimeEntryDto
                    {
                        Id = created.Id,
                        CustomerId = dto.CustomerId,
                        ProjectName = created.ProjectName ?? created.CustomerName ?? "",
                        Date = dto.Date,
                        Hours = dto.Hours,
                        Description = dto.Description,
                        CanEdit = true,
                        CanDelete = true
                    };
                }
            }

            throw new InvalidOperationException("Fehler beim Erstellen des Eintrags");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating time entry");
            throw new InvalidOperationException("Fehler beim Speichern", ex);
        }
    }

    public async Task<TimeEntryDto> UpdateEntryAsync(Guid teamMemberId, int entryId, UpdateTimeEntryDto dto)
    {
        // Check if entry can be edited (max 14 days old)
        var maxEditDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14));
        if (dto.Date < maxEditDate)
        {
            throw new InvalidOperationException("Einträge können nur bis 14 Tage zurück bearbeitet werden");
        }

        try
        {
            var client = CreateRapportClient();

            var startTime = dto.Date.ToDateTime(new TimeOnly(8, 0));
            var endTime = startTime.AddHours((double)dto.Hours);

            var rapportEntry = new
            {
                id = entryId,
                startTime,
                endTime,
                description = dto.Description,
                isManual = true,
                customerId = dto.CustomerId,
                status = "Completed",
                teamMemberId
            };

            var response = await client.PutAsJsonAsync($"/api/rapport/entries/{entryId}", rapportEntry);

            if (response.IsSuccessStatusCode)
            {
                return new TimeEntryDto
                {
                    Id = entryId,
                    CustomerId = dto.CustomerId,
                    ProjectName = "Updated", // Wird beim nächsten Load richtig geladen
                    Date = dto.Date,
                    Hours = dto.Hours,
                    Description = dto.Description,
                    CanEdit = true,
                    CanDelete = true
                };
            }

            throw new InvalidOperationException("Fehler beim Aktualisieren");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating time entry");
            throw new InvalidOperationException("Fehler beim Aktualisieren", ex);
        }
    }

    public async Task DeleteEntryAsync(Guid teamMemberId, int entryId)
    {
        try
        {
            var client = CreateRapportClient();

            var response = await client.DeleteAsync($"/api/rapport/entries/{entryId}");

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException("Fehler beim Löschen");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting time entry");
            throw new InvalidOperationException("Fehler beim Löschen", ex);
        }
    }

    private HttpClient CreateRapportClient()
    {
        var client = _httpClientFactory.CreateClient();
        var rapportUrl = _configuration.GetValue<string>("ServiceUrls:Rapport") ?? "http://localhost:8082";
        client.BaseAddress = new Uri(rapportUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
        return client;
    }

    // DTOs for Rapport API responses
    private class RapportTimeEntry
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Description { get; set; }
        public string? CustomerName { get; set; }
        public string? ProjectName { get; set; }
        public bool IsDeleted { get; set; }
        public TimeSpan? Duration { get; set; }
    }
}
