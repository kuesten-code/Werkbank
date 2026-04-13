using Kuestencode.Werkbank.Recepta.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Recepta.Data.Repositories;

/// <summary>
/// Repository-Implementierung für Projekt-Zuteilungen zu Belegen.
/// </summary>
public class DocumentAllocationRepository : IDocumentAllocationRepository
{
    private readonly ReceptaDbContext _context;

    public DocumentAllocationRepository(ReceptaDbContext context)
    {
        _context = context;
    }

    public async Task<List<DocumentProjectAllocation>> GetByDocumentIdAsync(Guid documentId)
    {
        return await _context.DocumentProjectAllocations
            .Where(a => a.DocumentId == documentId)
            .ToListAsync();
    }

    public async Task<List<(Document Document, DocumentProjectAllocation Allocation)>> GetByProjectIdAsync(Guid projectId)
    {
        var results = await _context.DocumentProjectAllocations
            .Include(a => a.Document)
                .ThenInclude(d => d.Supplier)
            .Where(a => a.ProjectId == projectId)
            .OrderByDescending(a => a.Document.InvoiceDate)
            .ThenByDescending(a => a.Document.CreatedAt)
            .ToListAsync();

        return results.Select(a => (a.Document, a)).ToList();
    }

    public async Task SetAllocationsAsync(Guid documentId, IEnumerable<DocumentProjectAllocation> allocations)
    {
        var existing = await _context.DocumentProjectAllocations
            .Where(a => a.DocumentId == documentId)
            .ToListAsync();

        _context.DocumentProjectAllocations.RemoveRange(existing);

        var newAllocations = allocations.ToList();
        if (newAllocations.Count > 0)
        {
            await _context.DocumentProjectAllocations.AddRangeAsync(newAllocations);
        }

        await _context.SaveChangesAsync();
    }
}
