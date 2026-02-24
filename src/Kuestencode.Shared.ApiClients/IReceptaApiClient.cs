using Kuestencode.Shared.Contracts.Recepta;

namespace Kuestencode.Shared.ApiClients;

/// <summary>
/// API client interface for communication with the Recepta module.
/// </summary>
public interface IReceptaApiClient
{
    Task<bool> CheckHealthAsync();
    Task<ProjectExpensesResponseDto?> GetProjectExpensesAsync(Guid projectId);
    Task<List<ReceptaDocumentDto>> GetDocumentsByProjectAsync(Guid projectId, bool onlyUnattached = false);
    Task<bool> MarkDocumentsAsAttachedAsync(IEnumerable<Guid> documentIds);
    Task<List<ReceptaDocumentFileDto>> GetFilesByDocumentAsync(Guid documentId);
    Task<(byte[] Data, string FileName, string ContentType)?> DownloadFileAsync(Guid fileId);
}
