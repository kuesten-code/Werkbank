using System.Net.Http.Json;
using Kuestencode.Shared.Contracts.Acta;

namespace Kuestencode.Shared.ApiClients;

/// <summary>
/// HTTP client implementation for interacting with the Acta service.
/// </summary>
public class ActaApiClient : IActaApiClient
{
    private readonly HttpClient _httpClient;

    public ActaApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/acta/health").ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<ActaProjectDto>> GetProjectsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/acta/projects/external").ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return [];
            return await response.Content.ReadFromJsonAsync<List<ActaProjectDto>>().ConfigureAwait(false) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<ActaProjectDto?> GetProjectByExternalIdAsync(int externalId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/acta/projects/external/{externalId}").ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<ActaProjectDto>().ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }
}
