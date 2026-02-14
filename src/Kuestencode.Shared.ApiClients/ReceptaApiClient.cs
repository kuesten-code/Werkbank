using System.Net.Http.Json;
using Kuestencode.Shared.Contracts.Recepta;

namespace Kuestencode.Shared.ApiClients;

/// <summary>
/// HTTP client implementation for interacting with the Recepta service.
/// </summary>
public class ReceptaApiClient : IReceptaApiClient
{
    private readonly HttpClient _httpClient;

    public ReceptaApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/recepta/health").ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<ProjectExpensesResponseDto?> GetProjectExpensesAsync(Guid projectId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/recepta/documents/project/{projectId}/expenses").ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<ProjectExpensesResponseDto>().ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<ReceptaDocumentDto>> GetDocumentsByProjectAsync(Guid projectId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/recepta/documents/project/{projectId}").ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return [];
            return await response.Content.ReadFromJsonAsync<List<ReceptaDocumentDto>>().ConfigureAwait(false) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
