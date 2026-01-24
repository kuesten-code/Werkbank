namespace Kuestencode.Shared.ApiClients;

public interface IRapportApiClient
{
    Task<bool> IsHealthyAsync();
}
