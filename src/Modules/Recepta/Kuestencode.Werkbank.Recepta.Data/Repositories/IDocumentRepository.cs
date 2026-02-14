using Kuestencode.Werkbank.Recepta.Domain.Entities;
using Kuestencode.Werkbank.Recepta.Domain.Enums;

namespace Kuestencode.Werkbank.Recepta.Data.Repositories;

/// <summary>
/// Repository für Belege.
/// </summary>
public interface IDocumentRepository
{
    /// <summary>
    /// Lädt einen Beleg mit allen zugehörigen Daten.
    /// </summary>
    Task<Document?> GetByIdAsync(Guid id);

    /// <summary>
    /// Lädt alle Belege mit optionalem Filter.
    /// </summary>
    Task<List<Document>> GetAllAsync(
        DocumentStatus? status = null,
        DocumentCategory? category = null,
        Guid? supplierId = null,
        Guid? projectId = null);

    /// <summary>
    /// Fügt einen neuen Beleg hinzu.
    /// </summary>
    Task AddAsync(Document document);

    /// <summary>
    /// Aktualisiert einen Beleg.
    /// </summary>
    Task UpdateAsync(Document document);

    /// <summary>
    /// Löscht einen Beleg (nur bei Status Draft erlaubt).
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Prüft, ob eine Belegnummer bereits existiert.
    /// </summary>
    Task<bool> ExistsNumberAsync(string documentNumber);

    /// <summary>
    /// Generiert die nächste verfügbare Belegnummer.
    /// Format: ER-YYYY-NNNN (z.B. ER-2026-0001)
    /// </summary>
    Task<string> GenerateDocumentNumberAsync();
}
