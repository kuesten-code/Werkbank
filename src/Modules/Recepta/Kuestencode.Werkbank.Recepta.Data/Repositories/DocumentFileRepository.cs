using Kuestencode.Werkbank.Recepta.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Recepta.Data.Repositories;

/// <summary>
/// Repository-Implementierung für Dateianhänge.
/// </summary>
public class DocumentFileRepository : IDocumentFileRepository
{
    private readonly ReceptaDbContext _context;

    public DocumentFileRepository(ReceptaDbContext context)
    {
        _context = context;
    }

    public async Task<DocumentFile?> GetByIdAsync(Guid id)
    {
        return await _context.DocumentFiles.FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<List<DocumentFile>> GetByDocumentIdAsync(Guid documentId)
    {
        return await _context.DocumentFiles
            .Where(f => f.DocumentId == documentId)
            .OrderBy(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(DocumentFile file)
    {
        await _context.DocumentFiles.AddAsync(file);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var file = await _context.DocumentFiles.FindAsync(id);
        if (file == null)
        {
            throw new InvalidOperationException($"Datei mit ID {id} nicht gefunden.");
        }

        _context.DocumentFiles.Remove(file);
        await _context.SaveChangesAsync();
    }
}
