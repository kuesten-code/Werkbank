using Kuestencode.Werkbank.Recepta.Data.Repositories;
using Kuestencode.Werkbank.Recepta.Domain.Dtos;
using Kuestencode.Werkbank.Recepta.Domain.Entities;

namespace Kuestencode.Werkbank.Recepta.Services;

/// <summary>
/// Service-Implementierung für Dateianhänge.
/// Speicherung: /app/data/{year}/{documentId}/{fileName}
/// </summary>
public class DocumentFileService : IDocumentFileService
{
    private readonly IDocumentFileRepository _fileRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly ILogger<DocumentFileService> _logger;
    private readonly string _storagePath;

    public DocumentFileService(
        IDocumentFileRepository fileRepository,
        IDocumentRepository documentRepository,
        ILogger<DocumentFileService> logger,
        IConfiguration configuration)
    {
        _fileRepository = fileRepository;
        _documentRepository = documentRepository;
        _logger = logger;
        _storagePath = configuration.GetValue<string>("FileStorage:Path") ?? "/app/data";
    }

    public async Task<DocumentFileDto> UploadAsync(Guid documentId, Stream file, string fileName, string contentType)
    {
        var document = await _documentRepository.GetByIdAsync(documentId);
        if (document == null)
        {
            throw new InvalidOperationException($"Beleg mit ID {documentId} nicht gefunden.");
        }

        if (!document.IsEditable)
        {
            throw new InvalidOperationException(
                $"Dateianhänge können nur bei Belegen im Status 'Draft' geändert werden. Status: {document.Status}");
        }

        var normalizedContentType = UploadPolicy.NormalizeContentType(contentType)
            ?? throw new InvalidOperationException("Nur PDF, JPEG, PNG, TIFF und XML sind erlaubt.");
        var safeName = UploadPolicy.SanitizeFileName(fileName);

        var directory = Path.GetFullPath(Path.Combine(_storagePath, DateTime.UtcNow.Year.ToString(), documentId.ToString()));
        Directory.CreateDirectory(directory);
        var storagePath = UniquePathIn(directory, safeName);

        long fileSize;
        try
        {
            await using var fileStream = File.Create(storagePath);
            fileSize = await CopyWithLimitAsync(file, fileStream);
        }
        catch (InvalidOperationException)
        {
            File.Delete(storagePath);
            throw;
        }

        var documentFile = new DocumentFile
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            FileName = safeName,
            ContentType = normalizedContentType,
            FileSize = fileSize,
            StoragePath = storagePath
        };

        await _fileRepository.AddAsync(documentFile);
        _logger.LogInformation("Datei '{FileName}' für Beleg {DocumentId} gespeichert unter {Path}",
            safeName, documentId, storagePath);

        return MapToDto(documentFile);
    }

    public async Task<List<DocumentFileDto>> GetByDocumentIdAsync(Guid documentId)
    {
        var files = await _fileRepository.GetByDocumentIdAsync(documentId);
        return files.Select(MapToDto).ToList();
    }

    public async Task<(Stream Content, string FileName, string ContentType)> DownloadAsync(Guid fileId)
    {
        var file = await _fileRepository.GetByIdAsync(fileId);
        if (file == null)
        {
            throw new InvalidOperationException($"Datei mit ID {fileId} nicht gefunden.");
        }

        if (!File.Exists(file.StoragePath))
        {
            throw new InvalidOperationException("Datei nicht auf dem Server gefunden.");
        }

        var stream = File.OpenRead(file.StoragePath);
        return (stream, file.FileName, file.ContentType);
    }

    public async Task DeleteAsync(Guid fileId)
    {
        var file = await _fileRepository.GetByIdAsync(fileId);
        if (file == null)
        {
            throw new InvalidOperationException($"Datei mit ID {fileId} nicht gefunden.");
        }

        var document = await _documentRepository.GetByIdAsync(file.DocumentId);
        if (document != null && !document.IsEditable)
        {
            throw new InvalidOperationException(
                $"Dateianhänge können nur bei Belegen im Status 'Draft' geändert werden. Status: {document.Status}");
        }

        if (File.Exists(file.StoragePath))
        {
            File.Delete(file.StoragePath);
        }

        await _fileRepository.DeleteAsync(fileId);
        _logger.LogInformation("Datei '{FileName}' gelöscht", file.FileName);
    }

    private static string UniquePathIn(string directory, string fileName)
    {
        var candidate = Path.GetFullPath(Path.Combine(directory, fileName));
        if (!candidate.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidOperationException("Ungültiger Dateiname.");

        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        for (var counter = 1; File.Exists(candidate); counter++)
            candidate = Path.Combine(directory, $"{nameWithoutExt}_{counter}{extension}");

        return candidate;
    }

    private static async Task<long> CopyWithLimitAsync(Stream source, Stream target)
    {
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await source.ReadAsync(buffer)) > 0)
        {
            total += read;
            if (total > UploadPolicy.MaxFileSize)
                throw new InvalidOperationException($"Datei darf maximal {UploadPolicy.MaxFileSize / 1024 / 1024} MB groß sein.");
            await target.WriteAsync(buffer.AsMemory(0, read));
        }
        return total;
    }

    private static DocumentFileDto MapToDto(DocumentFile file)
    {
        return new DocumentFileDto
        {
            Id = file.Id,
            DocumentId = file.DocumentId,
            FileName = file.FileName,
            ContentType = file.ContentType,
            FileSize = file.FileSize,
            CreatedAt = file.CreatedAt
        };
    }
}
