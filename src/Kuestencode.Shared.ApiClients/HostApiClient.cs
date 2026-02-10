using System.Net.Http.Json;
using Kuestencode.Shared.Contracts.Acta;
using Kuestencode.Shared.Contracts.Host;
using Kuestencode.Shared.Contracts.Navigation;
using Kuestencode.Shared.Contracts.Rapport;

namespace Kuestencode.Shared.ApiClients;

public class HostApiClient : IHostApiClient
{
    private readonly HttpClient _httpClient;

    public HostApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CompanyDto?> GetCompanyAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/company").ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<CompanyDto>().ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    public async Task UpdateCompanyAsync(UpdateCompanyRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync("/api/company", request).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task<CustomerDto?> GetCustomerAsync(int customerId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/customer/{customerId}").ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<CustomerDto>().ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<CustomerDto>> GetAllCustomersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/customer").ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<CustomerDto>>().ConfigureAwait(false) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<NavItemDto>> GetNavigationAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/navigation").ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<NavItemDto>>().ConfigureAwait(false) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<List<TeamMemberDto>> GetTeamMembersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/team-members").ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<TeamMemberDto>>().ConfigureAwait(false) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<ProjectHoursResponseDto?> GetProjectHoursAsync(int projectId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/rapport/projects/{projectId}/hours").ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<ProjectHoursResponseDto>().ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<ActaProjectDto>> GetActaProjectsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/acta-proxy/projects").ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return [];
            return await response.Content.ReadFromJsonAsync<List<ActaProjectDto>>().ConfigureAwait(false) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<ActaProjectDto?> GetActaProjectAsync(int externalId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/acta-proxy/projects/{externalId}").ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<ActaProjectDto>().ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }
}
