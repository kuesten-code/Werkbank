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
}
