using Kuestencode.Shared.Contracts.Recepta;

namespace Kuestencode.Shared.ApiClients;

/// <summary>
/// API client interface for communication with the Recepta module.
/// </summary>
public interface IReceptaApiClient
{
    Task<bool> CheckHealthAsync();
    Task<ProjectExpensesResponseDto?> GetProjectExpensesAsync(Guid projectId);
    Task<List<ReceptaDocumentDto>> GetDocumentsByProjectAsync(Guid projectId);
}
