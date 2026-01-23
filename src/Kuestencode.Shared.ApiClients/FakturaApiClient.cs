using System.Net.Http.Json;
using Kuestencode.Shared.Contracts.Faktura;

namespace Kuestencode.Shared.ApiClients;

public class FakturaApiClient : IFakturaApiClient
{
    private readonly HttpClient _httpClient;

    public FakturaApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<InvoiceDto>> GetAllInvoicesAsync(InvoiceFilterDto? filter = null)
    {
        var query = BuildQueryString(filter);
        var response = await _httpClient.GetAsync($"/api/invoice{query}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<InvoiceDto>>() ?? [];
    }

    public async Task<InvoiceDto?> GetInvoiceAsync(int id)
    {
        var response = await _httpClient.GetAsync($"/api/invoice/{id}");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<InvoiceDto>();
    }

    public async Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/invoice", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InvoiceDto>()
            ?? throw new InvalidOperationException("Failed to create invoice");
    }

    public async Task UpdateInvoiceAsync(int id, UpdateInvoiceRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"/api/invoice/{id}", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteInvoiceAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"/api/invoice/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task SendInvoiceAsync(int id)
    {
        var response = await _httpClient.PostAsync($"/api/invoice/{id}/send", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task<byte[]> GenerateInvoicePdfAsync(int id)
    {
        var response = await _httpClient.GetAsync($"/api/invoice/{id}/pdf");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task MarkAsPaidAsync(int id, DateTime paidDate)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/invoice/{id}/mark-paid", new { paidDate });
        response.EnsureSuccessStatusCode();
    }

    private string BuildQueryString(InvoiceFilterDto? filter)
    {
        if (filter == null) return string.Empty;

        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(filter.Status))
            queryParams.Add($"status={Uri.EscapeDataString(filter.Status)}");
        if (filter.CustomerId.HasValue)
            queryParams.Add($"customerId={filter.CustomerId}");
        if (filter.FromDate.HasValue)
            queryParams.Add($"fromDate={filter.FromDate.Value:yyyy-MM-dd}");
        if (filter.ToDate.HasValue)
            queryParams.Add($"toDate={filter.ToDate.Value:yyyy-MM-dd}");
        if (!string.IsNullOrEmpty(filter.SearchTerm))
            queryParams.Add($"searchTerm={Uri.EscapeDataString(filter.SearchTerm)}");

        return queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
    }
}
