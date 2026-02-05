using System.Net.Http.Json;
using Kuestencode.Shared.Contracts.Rapport;

namespace Kuestencode.Shared.ApiClients;

public class RapportApiClient : IRapportApiClient
{
    private readonly HttpClient _httpClient;

    public RapportApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/health").ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<byte[]> GenerateTimesheetPdfAsync(TimesheetExportRequestDto request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/timesheets/pdf", request).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
    }

    public async Task<byte[]> GenerateTimesheetCsvAsync(TimesheetExportRequestDto request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/timesheets/csv", request).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
    }

    public async Task<ProjectHoursResponseDto?> GetProjectHoursAsync(int projectId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/dashboard/projects/{projectId}/hours").ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<ProjectHoursResponseDto>().ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }
}
