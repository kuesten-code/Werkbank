namespace Kuestencode.Shared.ApiClients;

/// <summary>
/// API client interface for communication with the Offerte module.
/// </summary>
public interface IOfferteApiClient
{
    /// <summary>
    /// Checks if the Offerte module is healthy and reachable.
    /// </summary>
    Task<bool> CheckHealthAsync();
}
