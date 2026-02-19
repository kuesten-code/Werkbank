using Kuestencode.Core.Interfaces;
using Kuestencode.Shared.ApiClients;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Models;
using Kuestencode.Werkbank.Host.Models.MobileRapport;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Kuestencode.Werkbank.Host.Services;

public class MobileRapportService : IMobileRapportService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICustomerService _customerService;
    private readonly IActaApiClient _actaApiClient;
    private readonly HostDbContext _hostDbContext;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MobileRapportService> _logger;

    public MobileRapportService(
        IHttpClientFactory httpClientFactory,
        ICustomerService customerService,
        IActaApiClient actaApiClient,
        HostDbContext hostDbContext,
        IJwtTokenService jwtTokenService,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        ILogger<MobileRapportService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _customerService = customerService;
        _actaApiClient = actaApiClient;
        _hostDbContext = hostDbContext;
        _jwtTokenService = jwtTokenService;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> IsProjectSelectionAvailableAsync(Guid teamMemberId)
    {
        try
        {
            return await _actaApiClient.CheckHealthAsync();
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<ProjectSelectDto>> GetProjectsAsync(Guid teamMemberId)
    {
        if (!await IsProjectSelectionAvailableAsync(teamMemberId))
            return new List<ProjectSelectDto>();

        var projects = await _actaApiClient.GetProjectsAsync();

        return projects
            .OrderBy(p => p.Name)
            .Select(p => new ProjectSelectDto
            {
                Id = p.Id,
                Name = p.Name,
                ProjectNumber = p.ProjectNumber,
                CustomerId = p.CustomerId,
                CustomerName = p.CustomerName
            })
            .ToList();
    }

    public async Task<List<CustomerSelectDto>> GetCustomersAsync(Guid teamMemberId)
    {
        try
        {
            var client = await CreateRapportClientAsync(teamMemberId);
            var response = await client.GetAsync("/api/rapport/customers");
            if (response.IsSuccessStatusCode)
            {
                var customersFromRapport = await response.Content.ReadFromJsonAsync<List<CustomerSelectDto>>();
                if (customersFromRapport != null)
                {
                    return customersFromRapport
                        .OrderBy(c => c.Name)
                        .ToList();
                }
            }
        }
        catch
        {
            // Fallback below.
        }

        var customers = await _customerService.GetAllAsync();
        return customers
            .OrderBy(c => c.Name)
            .Select(c => new CustomerSelectDto
            {
                Id = c.Id,
                Name = c.Name,
                CustomerNumber = c.CustomerNumber
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
            var client = await CreateRapportClientAsync(teamMemberId);

            var fromDateTime = from.ToDateTime(TimeOnly.MinValue);
            var toDateTime = to.ToDateTime(TimeOnly.MaxValue);
            var fromQuery = Uri.EscapeDataString(fromDateTime.ToString("yyyy-MM-ddTHH:mm:ss"));
            var toQuery = Uri.EscapeDataString(toDateTime.ToString("yyyy-MM-ddTHH:mm:ss"));

            var response = await client.GetAsync(
                $"/api/rapport/entries?from={fromQuery}&to={toQuery}&teamMemberId={teamMemberId}");

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
                        var localStart = ConvertUtcToConfiguredLocalTime(e.StartTime);
                        var localEnd = e.EndTime.HasValue
                            ? ConvertUtcToConfiguredLocalTime(e.EndTime.Value)
                            : (DateTime?)null;
                        var entryDate = DateOnly.FromDateTime(localStart);
                        return new TimeEntryDto
                        {
                            Id = e.Id,
                            CustomerId = e.CustomerId,
                            ProjectId = e.ProjectId,
                            CustomerName = e.CustomerName,
                            ProjectName = e.ProjectName,
                            Date = entryDate,
                            StartTime = localStart.TimeOfDay,
                            EndTime = localEnd?.TimeOfDay,
                            Hours = (decimal)(e.Duration?.TotalHours ?? 0),
                            Description = e.Description,
                            CanEdit = entryDate >= maxEditDate,
                            CanDelete = entryDate >= maxEditDate
                        };
                    })
                    .OrderByDescending(e => e.Date)
                    .ToList();
            }

            var error = await ReadErrorMessageAsync(response, "Fehler beim Laden der Zeiteinträge");
            throw new InvalidOperationException(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading time entries for team member {TeamMemberId}", teamMemberId);
            throw new InvalidOperationException("Fehler beim Laden der Zeiteinträge", ex);
        }
    }

    public async Task<TimeEntryDto> CreateEntryAsync(Guid teamMemberId, CreateTimeEntryDto dto)
    {
        if (!dto.ProjectId.HasValue && !dto.CustomerId.HasValue)
            throw new InvalidOperationException("Bitte wähle einen Kunden oder ein Projekt.");
        if (!dto.StartTime.HasValue || !dto.EndTime.HasValue)
            throw new InvalidOperationException("Bitte Start- und Endzeit wählen.");
        if (dto.EndTime <= dto.StartTime)
            throw new InvalidOperationException("Endzeit muss nach der Startzeit liegen.");

        try
        {
            var client = await CreateRapportClientAsync(teamMemberId);

            var startTime = ConvertConfiguredLocalTimeToUtc(dto.Date, dto.StartTime.Value);
            var endTime = ConvertConfiguredLocalTimeToUtc(dto.Date, dto.EndTime.Value);
            var hours = (decimal)(endTime - startTime).TotalHours;

            var rapportEntry = new
            {
                startTime,
                endTime,
                description = dto.Description,
                isManual = true,
                projectId = dto.ProjectId,
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
                        CustomerId = created.CustomerId,
                        ProjectId = created.ProjectId,
                        CustomerName = created.CustomerName,
                        ProjectName = created.ProjectName,
                        Date = dto.Date,
                        StartTime = dto.StartTime.Value,
                        EndTime = dto.EndTime.Value,
                        Hours = hours,
                        Description = dto.Description,
                        CanEdit = true,
                        CanDelete = true
                    };
                }
            }

            var error = await ReadErrorMessageAsync(response, "Fehler beim Erstellen des Eintrags");
            throw new InvalidOperationException(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating time entry");
            throw new InvalidOperationException(ex.Message, ex);
        }
    }

    public async Task<TimeEntryDto> UpdateEntryAsync(Guid teamMemberId, int entryId, UpdateTimeEntryDto dto)
    {
        if (!dto.ProjectId.HasValue && !dto.CustomerId.HasValue)
            throw new InvalidOperationException("Bitte wähle einen Kunden oder ein Projekt.");
        if (!dto.StartTime.HasValue || !dto.EndTime.HasValue)
            throw new InvalidOperationException("Bitte Start- und Endzeit wählen.");
        if (dto.EndTime <= dto.StartTime)
            throw new InvalidOperationException("Endzeit muss nach der Startzeit liegen.");

        var maxEditDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14));
        if (dto.Date < maxEditDate)
        {
            throw new InvalidOperationException("Einträge können nur bis 14 Tage zurück bearbeitet werden");
        }

        try
        {
            var client = await CreateRapportClientAsync(teamMemberId);

            var startTime = ConvertConfiguredLocalTimeToUtc(dto.Date, dto.StartTime.Value);
            var endTime = ConvertConfiguredLocalTimeToUtc(dto.Date, dto.EndTime.Value);
            var hours = (decimal)(endTime - startTime).TotalHours;

            var rapportEntry = new
            {
                id = entryId,
                startTime,
                endTime,
                description = dto.Description,
                isManual = true,
                projectId = dto.ProjectId,
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
                    CustomerId = dto.CustomerId ?? 0,
                    ProjectId = dto.ProjectId,
                    Date = dto.Date,
                    StartTime = dto.StartTime.Value,
                    EndTime = dto.EndTime.Value,
                    Hours = hours,
                    Description = dto.Description,
                    CanEdit = true,
                    CanDelete = true
                };
            }

            var error = await ReadErrorMessageAsync(response, "Fehler beim Aktualisieren");
            throw new InvalidOperationException(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating time entry");
            throw new InvalidOperationException(ex.Message, ex);
        }
    }

    public async Task DeleteEntryAsync(Guid teamMemberId, int entryId)
    {
        try
        {
            var client = await CreateRapportClientAsync(teamMemberId);

            var response = await client.DeleteAsync($"/api/rapport/entries/{entryId}");

            if (!response.IsSuccessStatusCode)
            {
                var error = await ReadErrorMessageAsync(response, "Fehler beim Löschen");
                throw new InvalidOperationException(error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting time entry");
            throw new InvalidOperationException(ex.Message, ex);
        }
    }

    public async Task<List<MobileTaskDto>> GetAssignedTasksAsync(Guid teamMemberId, bool includeCompleted = true)
    {
        try
        {
            var client = await CreateActaClientAsync(teamMemberId);
            var response = await client.GetAsync($"/api/acta/tasks/assigned/{teamMemberId}");

            if (response.IsSuccessStatusCode)
            {
                var tasks = await response.Content.ReadFromJsonAsync<List<ActaAssignedTaskDto>>();
                if (tasks == null)
                {
                    return new List<MobileTaskDto>();
                }

                var customers = (await _customerService.GetAllAsync()).ToDictionary(c => c.Id);

                return tasks
                    .Where(t => includeCompleted || !string.Equals(t.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(t => string.Equals(t.Status, "Completed", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                    .ThenBy(t => t.TargetDate)
                    .ThenBy(t => t.ProjectName)
                    .ThenBy(t => t.SortOrder)
                    .Select(t =>
                    {
                        customers.TryGetValue(t.CustomerId, out var customer);
                        return new MobileTaskDto
                        {
                            Id = t.Id,
                            ProjectId = t.ProjectId,
                            ProjectExternalId = t.ProjectExternalId,
                            CustomerId = t.CustomerId,
                            CustomerName = customer?.Name,
                            CustomerAddress = customer?.Address,
                            CustomerPostalCode = customer?.PostalCode,
                            CustomerCity = customer?.City,
                            ProjectName = t.ProjectName ?? "Unbekanntes Projekt",
                            ProjectNumber = t.ProjectNumber,
                            ProjectAddress = t.ProjectAddress,
                            ProjectPostalCode = t.ProjectPostalCode,
                            ProjectCity = t.ProjectCity,
                            Title = t.Title ?? string.Empty,
                            Notes = t.Notes,
                            Status = string.IsNullOrWhiteSpace(t.Status) ? "Open" : t.Status,
                            TargetDate = t.TargetDate,
                            CompletedAt = t.CompletedAt,
                            SortOrder = t.SortOrder
                        };
                    })
                    .ToList();
            }

            var error = await ReadErrorMessageAsync(response, "Fehler beim Laden der Aufgaben");
            throw new InvalidOperationException(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading assigned tasks for team member {TeamMemberId}", teamMemberId);
            throw new InvalidOperationException("Fehler beim Laden der Aufgaben", ex);
        }
    }

    public async Task<MobileTaskDto> SetTaskCompletedAsync(Guid teamMemberId, Guid taskId, bool completed)
    {
        try
        {
            var client = await CreateActaClientAsync(teamMemberId);
            var action = completed ? "complete" : "reopen";
            var response = await client.PostAsync($"/api/acta/tasks/{taskId}/{action}", content: null);

            if (response.IsSuccessStatusCode)
            {
                var task = await response.Content.ReadFromJsonAsync<ActaAssignedTaskDto>();
                if (task == null)
                {
                    throw new InvalidOperationException("Aufgabe konnte nicht geladen werden.");
                }

                var customer = await ResolveCustomerAsync(task.CustomerId);

                return new MobileTaskDto
                {
                    Id = task.Id,
                    ProjectId = task.ProjectId,
                    ProjectExternalId = task.ProjectExternalId,
                    CustomerId = task.CustomerId,
                    CustomerName = customer?.Name,
                    CustomerAddress = customer?.Address,
                    CustomerPostalCode = customer?.PostalCode,
                    CustomerCity = customer?.City,
                    ProjectName = task.ProjectName ?? "Unbekanntes Projekt",
                    ProjectNumber = task.ProjectNumber,
                    ProjectAddress = task.ProjectAddress,
                    ProjectPostalCode = task.ProjectPostalCode,
                    ProjectCity = task.ProjectCity,
                    Title = task.Title ?? string.Empty,
                    Notes = task.Notes,
                    Status = string.IsNullOrWhiteSpace(task.Status) ? "Open" : task.Status,
                    TargetDate = task.TargetDate,
                    CompletedAt = task.CompletedAt,
                    SortOrder = task.SortOrder
                };
            }

            var error = await ReadErrorMessageAsync(response, "Fehler beim Aktualisieren der Aufgabe");
            throw new InvalidOperationException(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating task {TaskId} for team member {TeamMemberId}", taskId, teamMemberId);
            throw new InvalidOperationException("Fehler beim Aktualisieren der Aufgabe", ex);
        }
    }

    private async Task<Kuestencode.Core.Models.Customer?> ResolveCustomerAsync(int customerId)
    {
        if (customerId <= 0)
        {
            return null;
        }

        try
        {
            return await _customerService.GetByIdAsync(customerId);
        }
        catch
        {
            return null;
        }
    }

    private async Task<HttpClient> CreateRapportClientAsync(Guid teamMemberId)
    {
        var client = _httpClientFactory.CreateClient();
        var rapportUrl = _configuration.GetValue<string>("ServiceUrls:Rapport") ?? "http://localhost:8082";
        client.BaseAddress = new Uri(rapportUrl);
        client.Timeout = TimeSpan.FromSeconds(30);

        var token = ExtractToken(_httpContextAccessor.HttpContext);
        if (string.IsNullOrWhiteSpace(token))
        {
            token = await GenerateTokenForTeamMemberAsync(teamMemberId);
        }

        if (!string.IsNullOrWhiteSpace(token))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return client;
    }

    private async Task<HttpClient> CreateActaClientAsync(Guid teamMemberId)
    {
        var client = _httpClientFactory.CreateClient();
        var actaUrl = _configuration.GetValue<string>("ServiceUrls:Acta") ?? "http://localhost:8084";
        client.BaseAddress = new Uri(actaUrl);
        client.Timeout = TimeSpan.FromSeconds(30);

        var token = ExtractToken(_httpContextAccessor.HttpContext);
        if (string.IsNullOrWhiteSpace(token))
        {
            token = await GenerateTokenForTeamMemberAsync(teamMemberId);
        }

        if (!string.IsNullOrWhiteSpace(token))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return client;
    }

    private static string? ExtractToken(HttpContext? httpContext)
    {
        if (httpContext == null)
            return null;

        var authHeader = httpContext.Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(authHeader) &&
            authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader["Bearer ".Length..].Trim();
        }

        if (httpContext.Request.Cookies.TryGetValue("werkbank_auth_cookie", out var cookieToken))
        {
            return cookieToken;
        }

        return null;
    }

    private async Task<string?> GenerateTokenForTeamMemberAsync(Guid teamMemberId)
    {
        if (teamMemberId == Guid.Empty)
            return null;

        TeamMember? member;
        try
        {
            member = await _hostDbContext.TeamMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == teamMemberId && m.IsActive);
        }
        catch
        {
            return null;
        }

        if (member == null)
            return null;

        return _jwtTokenService.GenerateToken(member);
    }

    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, string fallback)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
                return fallback;

            using var json = System.Text.Json.JsonDocument.Parse(content);
            var root = json.RootElement;
            if (root.TryGetProperty("error", out var errorElement))
            {
                var message = errorElement.GetString();
                if (!string.IsNullOrWhiteSpace(message))
                    return message;
            }

            return $"{fallback} (HTTP {(int)response.StatusCode})";
        }
        catch
        {
            return $"{fallback} (HTTP {(int)response.StatusCode})";
        }
    }

    private DateTime ConvertConfiguredLocalTimeToUtc(DateOnly date, TimeSpan timeOfDay)
    {
        var localUnspecified = DateTime.SpecifyKind(
            date.ToDateTime(TimeOnly.FromTimeSpan(timeOfDay)),
            DateTimeKind.Unspecified);

        var timeZone = ResolveEntryTimeZone();
        return TimeZoneInfo.ConvertTimeToUtc(localUnspecified, timeZone);
    }

    private DateTime ConvertUtcToConfiguredLocalTime(DateTime utcDateTime)
    {
        var utc = utcDateTime.Kind switch
        {
            DateTimeKind.Utc => utcDateTime,
            DateTimeKind.Local => utcDateTime.ToUniversalTime(),
            _ => DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc)
        };

        var timeZone = ResolveEntryTimeZone();
        return TimeZoneInfo.ConvertTimeFromUtc(utc, timeZone);
    }

    private TimeZoneInfo ResolveEntryTimeZone()
    {
        var configured = _configuration["TimeZone:Default"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(configured);
            }
            catch
            {
                _logger.LogWarning("Configured timezone '{ConfiguredTimeZone}' not found. Falling back.", configured);
            }
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
        }
        catch
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
            }
            catch
            {
                return TimeZoneInfo.Local;
            }
        }
    }

    private class RapportTimeEntry
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int? ProjectId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Description { get; set; }
        public string? CustomerName { get; set; }
        public string? ProjectName { get; set; }
        public bool IsDeleted { get; set; }
        public TimeSpan? Duration { get; set; }
    }

    private class ActaAssignedTaskDto
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public int? ProjectExternalId { get; set; }
        public int CustomerId { get; set; }
        public string? ProjectName { get; set; }
        public string? ProjectNumber { get; set; }
        public string? ProjectAddress { get; set; }
        public string? ProjectPostalCode { get; set; }
        public string? ProjectCity { get; set; }
        public string? Title { get; set; }
        public string? Notes { get; set; }
        public string? Status { get; set; }
        public DateOnly? TargetDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int SortOrder { get; set; }
    }
}
