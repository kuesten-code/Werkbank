using Kuestencode.Werkbank.Recepta.Domain.Entities;

namespace Kuestencode.Werkbank.Recepta.Data.Repositories;

/// <summary>
/// Repository für OCR-Muster.
/// </summary>
public interface ISupplierOcrPatternRepository
{
    /// <summary>
    /// Lädt alle OCR-Muster eines Lieferanten.
    /// </summary>
    Task<List<SupplierOcrPattern>> GetBySupplerIdAsync(Guid supplierId);

    /// <summary>
    /// Lädt ein OCR-Muster anhand von Lieferant und Feldname.
    /// </summary>
    Task<SupplierOcrPattern?> GetBySupplierIdAndFieldNameAsync(Guid supplierId, string fieldName);

    /// <summary>
    /// Fügt ein neues OCR-Muster hinzu.
    /// </summary>
    Task AddAsync(SupplierOcrPattern pattern);

    /// <summary>
    /// Aktualisiert ein OCR-Muster.
    /// </summary>
    Task UpdateAsync(SupplierOcrPattern pattern);

    /// <summary>
    /// Löscht ein OCR-Muster.
    /// </summary>
    Task DeleteAsync(Guid id);
}
