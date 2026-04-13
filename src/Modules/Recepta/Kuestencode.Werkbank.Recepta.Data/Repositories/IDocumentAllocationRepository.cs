using Kuestencode.Werkbank.Recepta.Domain.Entities;

namespace Kuestencode.Werkbank.Recepta.Data.Repositories;

/// <summary>
/// Repository für Projekt-Zuteilungen zu Belegen.
/// </summary>
public interface IDocumentAllocationRepository
{
    /// <summary>
    /// Lädt alle Zuteilungen eines Belegs.
    /// </summary>
    Task<List<DocumentProjectAllocation>> GetByDocumentIdAsync(Guid documentId);

    /// <summary>
    /// Lädt alle Belege mit ihrer jeweiligen Zuteilung für ein Projekt.
    /// </summary>
    Task<List<(Document Document, DocumentProjectAllocation Allocation)>> GetByProjectIdAsync(Guid projectId);

    /// <summary>
    /// Ersetzt alle Zuteilungen eines Belegs atomisch.
    /// Bestehende Zeilen werden gelöscht und die neuen eingefügt.
    /// </summary>
    Task SetAllocationsAsync(Guid documentId, IEnumerable<DocumentProjectAllocation> allocations);
}
