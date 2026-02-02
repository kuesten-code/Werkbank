namespace Kuestencode.Shared.ApiClients;

/// <summary>
/// HTTP client implementation for communication with the Offerte module.
/// </summary>
public class OfferteApiClient : IOfferteApiClient
{
    private readonly HttpClient _httpClient;

    public OfferteApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/offerte/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
