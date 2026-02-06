namespace Kuestencode.Shared.ApiClients;

/// <summary>
/// API client interface for communication with the Acta module.
/// </summary>
public interface IActaApiClient
{
    /// <summary>
    /// Checks whether the Acta module reports a healthy status.
    /// </summary>
    Task<bool> CheckHealthAsync();
}
