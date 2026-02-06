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
}
