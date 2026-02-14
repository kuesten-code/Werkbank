using Kuestencode.Werkbank.Recepta.Domain.Entities;

namespace Kuestencode.Werkbank.Recepta.Data.Repositories;

/// <summary>
/// Repository für Lieferanten.
/// </summary>
public interface ISupplierRepository
{
    /// <summary>
    /// Lädt einen Lieferanten mit allen zugehörigen Daten.
    /// </summary>
    Task<Supplier?> GetByIdAsync(Guid id);

    /// <summary>
    /// Lädt einen Lieferanten anhand der Lieferantennummer.
    /// </summary>
    Task<Supplier?> GetByNumberAsync(string supplierNumber);

    /// <summary>
    /// Lädt alle Lieferanten mit optionaler Suche.
    /// </summary>
    Task<List<Supplier>> GetAllAsync(string? search = null);

    /// <summary>
    /// Sucht einen Lieferanten anhand des Namens (exakte oder Teilsuche).
    /// </summary>
    Task<Supplier?> FindByNameAsync(string name);

    /// <summary>
    /// Sucht einen Lieferanten anhand der Umsatzsteuer-ID.
    /// </summary>
    Task<Supplier?> FindByTaxIdAsync(string taxId);

    /// <summary>
    /// Sucht einen Lieferanten anhand der IBAN.
    /// </summary>
    Task<Supplier?> FindByIbanAsync(string iban);

    /// <summary>
    /// Fügt einen neuen Lieferanten hinzu.
    /// </summary>
    Task AddAsync(Supplier supplier);

    /// <summary>
    /// Aktualisiert einen Lieferanten.
    /// </summary>
    Task UpdateAsync(Supplier supplier);

    /// <summary>
    /// Löscht einen Lieferanten.
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Prüft, ob eine Lieferantennummer bereits existiert.
    /// </summary>
    Task<bool> ExistsNumberAsync(string supplierNumber);

    /// <summary>
    /// Generiert die nächste verfügbare Lieferantennummer.
    /// Format: L-NNNN (z.B. L-0001)
    /// </summary>
    Task<string> GenerateSupplierNumberAsync();
}
