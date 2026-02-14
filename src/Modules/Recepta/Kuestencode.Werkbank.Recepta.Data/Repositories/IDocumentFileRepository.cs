using Kuestencode.Werkbank.Recepta.Domain.Entities;

namespace Kuestencode.Werkbank.Recepta.Data.Repositories;

/// <summary>
/// Repository für Dateianhänge.
/// </summary>
public interface IDocumentFileRepository
{
    /// <summary>
    /// Lädt einen Dateianhang anhand der ID.
    /// </summary>
    Task<DocumentFile?> GetByIdAsync(Guid id);

    /// <summary>
    /// Lädt alle Dateianhänge eines Belegs.
    /// </summary>
    Task<List<DocumentFile>> GetByDocumentIdAsync(Guid documentId);

    /// <summary>
    /// Fügt einen neuen Dateianhang hinzu.
    /// </summary>
    Task AddAsync(DocumentFile file);

    /// <summary>
    /// Löscht einen Dateianhang.
    /// </summary>
    Task DeleteAsync(Guid id);
}
