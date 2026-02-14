using Kuestencode.Werkbank.Recepta.Controllers.Dtos;
using Kuestencode.Werkbank.Recepta.Domain.Dtos;

namespace Kuestencode.Werkbank.Recepta.Services;

/// <summary>
/// Service für Lieferantenverwaltung.
/// </summary>
public interface ISupplierService
{
    /// <summary>
    /// Lädt alle Lieferanten mit optionaler Suche.
    /// </summary>
    Task<IEnumerable<SupplierDto>> GetAllAsync(string? search = null);

    /// <summary>
    /// Lädt einen Lieferanten anhand der ID.
    /// </summary>
    Task<SupplierDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Erstellt einen neuen Lieferanten.
    /// </summary>
    Task<SupplierDto> CreateAsync(CreateSupplierDto dto);

    /// <summary>
    /// Aktualisiert einen Lieferanten.
    /// </summary>
    Task<SupplierDto> UpdateAsync(Guid id, UpdateSupplierDto dto);

    /// <summary>
    /// Löscht einen Lieferanten (nur wenn keine Belege verknüpft).
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Sucht einen Lieferanten anhand des Namens (für OCR-Vorschlag).
    /// </summary>
    Task<SupplierDto?> FindByNameAsync(string name);

    /// <summary>
    /// Generiert die nächste verfügbare Lieferantennummer.
    /// </summary>
    Task<string> GenerateSupplierNumberAsync();
}
