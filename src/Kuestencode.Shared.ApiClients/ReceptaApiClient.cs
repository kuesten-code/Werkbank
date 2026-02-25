using System.Net.Http.Json;
using Kuestencode.Shared.Contracts.Recepta;

namespace Kuestencode.Shared.ApiClients;

/// <summary>
/// HTTP client implementation for interacting with the Recepta service.
/// </summary>
public class ReceptaApiClient : IReceptaApiClient
{
    private readonly HttpClient _httpClient;

    public ReceptaApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/recepta/health").ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<ProjectExpensesResponseDto?> GetProjectExpensesAsync(Guid projectId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/recepta/documents/project/{projectId}/expenses").ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<ProjectExpensesResponseDto>().ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<ReceptaDocumentDto>> GetDocumentsByProjectAsync(Guid projectId, bool onlyUnattached = false)
    {
        try
        {
            var path = $"/api/recepta/documents/project/{projectId}";
            if (onlyUnattached)
            {
                path += "?onlyUnattached=true";
            }

            var response = await _httpClient.GetAsync(path).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return [];
            return await response.Content.ReadFromJsonAsync<List<ReceptaDocumentDto>>().ConfigureAwait(false) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<bool> MarkDocumentsAsAttachedAsync(IEnumerable<Guid> documentIds)
    {
        try
        {
            var request = new MarkDocumentsAttachedRequestDto
            {
                DocumentIds = documentIds.Distinct().ToList()
            };

            var response = await _httpClient.PostAsJsonAsync("/api/recepta/documents/mark-attached", request)
                .ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<ReceptaDocumentFileDto>> GetFilesByDocumentAsync(Guid documentId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/recepta/documents/{documentId}/files").ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return [];
            return await response.Content.ReadFromJsonAsync<List<ReceptaDocumentFileDto>>().ConfigureAwait(false) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<(byte[] Data, string FileName, string ContentType)?> DownloadFileAsync(Guid fileId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/recepta/files/{fileId}").ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return null;

            var data = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
            var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                ?? response.Content.Headers.ContentDisposition?.FileName
                ?? $"{fileId}.bin";

            fileName = fileName.Trim('"');
            return (data, fileName, contentType);
        }
        catch
        {
            return null;
        }
    }
}
