using Kuestencode.Werkbank.Recepta.Domain.Dtos;

namespace Kuestencode.Werkbank.Recepta.Services;

/// <summary>
/// Service für Dateianhänge.
/// </summary>
public interface IDocumentFileService
{
    /// <summary>
    /// Lädt einen Dateianhang hoch.
    /// Speicherung: /app/data/{year}/{documentId}/{fileName}
    /// </summary>
    Task<DocumentFileDto> UploadAsync(Guid documentId, Stream file, string fileName, string contentType);

    /// <summary>
    /// Lädt alle Dateianhänge eines Belegs.
    /// </summary>
    Task<List<DocumentFileDto>> GetByDocumentIdAsync(Guid documentId);

    /// <summary>
    /// Lädt einen Dateianhang herunter.
    /// </summary>
    Task<(Stream Content, string FileName, string ContentType)> DownloadAsync(Guid fileId);

    /// <summary>
    /// Löscht einen Dateianhang.
    /// </summary>
    Task DeleteAsync(Guid fileId);
}
