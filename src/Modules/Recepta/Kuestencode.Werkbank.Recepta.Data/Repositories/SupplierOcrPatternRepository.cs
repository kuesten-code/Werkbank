using Kuestencode.Werkbank.Recepta.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Recepta.Data.Repositories;

/// <summary>
/// Repository-Implementierung für OCR-Muster.
/// </summary>
public class SupplierOcrPatternRepository : ISupplierOcrPatternRepository
{
    private readonly ReceptaDbContext _context;

    public SupplierOcrPatternRepository(ReceptaDbContext context)
    {
        _context = context;
    }

    public async Task<List<SupplierOcrPattern>> GetBySupplerIdAsync(Guid supplierId)
    {
        return await _context.SupplierOcrPatterns
            .Where(p => p.SupplierId == supplierId)
            .OrderBy(p => p.FieldName)
            .ToListAsync();
    }

    public async Task<SupplierOcrPattern?> GetBySupplierIdAndFieldNameAsync(Guid supplierId, string fieldName)
    {
        return await _context.SupplierOcrPatterns
            .FirstOrDefaultAsync(p => p.SupplierId == supplierId && p.FieldName == fieldName);
    }

    public async Task AddAsync(SupplierOcrPattern pattern)
    {
        await _context.SupplierOcrPatterns.AddAsync(pattern);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(SupplierOcrPattern pattern)
    {
        _context.SupplierOcrPatterns.Update(pattern);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var pattern = await _context.SupplierOcrPatterns.FindAsync(id);
        if (pattern == null)
        {
            throw new InvalidOperationException($"OCR-Muster mit ID {id} nicht gefunden.");
        }

        _context.SupplierOcrPatterns.Remove(pattern);
        await _context.SaveChangesAsync();
    }
}
