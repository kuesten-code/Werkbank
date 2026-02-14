using Kuestencode.Werkbank.Recepta.Domain.Entities;
using Kuestencode.Werkbank.Recepta.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Recepta.Data.Repositories;

/// <summary>
/// Repository-Implementierung für Belege.
/// </summary>
public class DocumentRepository : IDocumentRepository
{
    private readonly ReceptaDbContext _context;

    public DocumentRepository(ReceptaDbContext context)
    {
        _context = context;
    }

    public async Task<Document?> GetByIdAsync(Guid id)
    {
        return await _context.Documents
            .Include(d => d.Supplier)
            .Include(d => d.Files)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<List<Document>> GetAllAsync(
        DocumentStatus? status = null,
        DocumentCategory? category = null,
        Guid? supplierId = null,
        Guid? projectId = null)
    {
        var query = _context.Documents
            .Include(d => d.Supplier)
            .Include(d => d.Files)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(d => d.Status == status.Value);
        }

        if (category.HasValue)
        {
            query = query.Where(d => d.Category == category.Value);
        }

        if (supplierId.HasValue)
        {
            query = query.Where(d => d.SupplierId == supplierId.Value);
        }

        if (projectId.HasValue)
        {
            query = query.Where(d => d.ProjectId == projectId.Value);
        }

        return await query
            .OrderByDescending(d => d.InvoiceDate)
            .ThenByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(Document document)
    {
        await _context.Documents.AddAsync(document);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Document document)
    {
        _context.Documents.Update(document);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var document = await _context.Documents.FindAsync(id);
        if (document == null)
        {
            throw new InvalidOperationException($"Beleg mit ID {id} nicht gefunden.");
        }

        if (document.Status != DocumentStatus.Draft)
        {
            throw new InvalidOperationException(
                "Beleg kann nicht gelöscht werden. Nur Belege im Status 'Draft' können gelöscht werden.");
        }

        _context.Documents.Remove(document);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsNumberAsync(string documentNumber)
    {
        return await _context.Documents.AnyAsync(d => d.DocumentNumber == documentNumber);
    }

    public async Task<string> GenerateDocumentNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"ER-{year}-";

        var lastNumber = await _context.Documents
            .Where(d => d.DocumentNumber.StartsWith(prefix))
            .OrderByDescending(d => d.DocumentNumber)
            .Select(d => d.DocumentNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastNumber != null)
        {
            var numberPart = lastNumber[prefix.Length..];
            if (int.TryParse(numberPart, out var num))
                nextNumber = num + 1;
        }

        return $"{prefix}{nextNumber:D4}";
    }
}
