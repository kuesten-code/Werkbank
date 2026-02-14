using Kuestencode.Werkbank.Recepta.Controllers.Dtos;
using Kuestencode.Werkbank.Recepta.Domain.Dtos;
using Kuestencode.Werkbank.Recepta.Domain.Enums;

namespace Kuestencode.Werkbank.Recepta.Services;

/// <summary>
/// Service für Belegverwaltung.
/// </summary>
public interface IDocumentService
{
    /// <summary>
    /// Lädt alle Belege mit optionalem Filter.
    /// </summary>
    Task<IEnumerable<DocumentDto>> GetAllAsync(DocumentFilterDto filter);

    /// <summary>
    /// Lädt einen Beleg anhand der ID.
    /// </summary>
    Task<DocumentDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Erstellt einen neuen Beleg.
    /// </summary>
    Task<DocumentDto> CreateAsync(CreateDocumentDto dto);

    /// <summary>
    /// Erstellt einen Beleg aus einem Scan (OCR + Pattern-Extraktion).
    /// </summary>
    Task<ScanResultDto> CreateFromScanAsync(Stream file, string fileName);

    /// <summary>
    /// Aktualisiert einen Beleg.
    /// </summary>
    Task<DocumentDto> UpdateAsync(Guid id, UpdateDocumentDto dto);

    /// <summary>
    /// Ändert den Status eines Belegs.
    /// </summary>
    Task ChangeStatusAsync(Guid id, DocumentStatus newStatus);

    /// <summary>
    /// Triggert den Lerneffekt: Lernt Patterns aus den aktuellen Beleg-Daten.
    /// </summary>
    Task LearnPatternsAsync(Guid id);

    /// <summary>
    /// Löscht einen Beleg (nur bei Status Draft).
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Generiert die nächste verfügbare Belegnummer.
    /// </summary>
    Task<string> GenerateDocumentNumberAsync();

    /// <summary>
    /// Aktualisiert den OCR-Rohtext eines Belegs.
    /// </summary>
    Task UpdateOcrTextAsync(Guid id, string ocrText);
}
