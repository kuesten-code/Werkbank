using System.Net.Http.Json;
using Kuestencode.Shared.Contracts.Host;
using Kuestencode.Shared.Contracts.Navigation;

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
            var response = await _httpClient.GetAsync("/api/company");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<CompanyDto>();
        }
        catch
        {
            return null;
        }
    }

    public async Task UpdateCompanyAsync(UpdateCompanyRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync("/api/company", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task<CustomerDto?> GetCustomerAsync(int customerId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/customer/{customerId}");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<CustomerDto>();
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
            var response = await _httpClient.GetAsync("/api/customer");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<CustomerDto>>() ?? [];
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
            var response = await _httpClient.GetAsync("/api/navigation");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<NavItemDto>>() ?? [];
        }
        catch
        {
            return [];
        }
    }
}
