using Kuestencode.Werkbank.Saldo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Saldo.Data.Repositories;

public class KategorieKontoMappingRepository : IKategorieKontoMappingRepository
{
    private readonly SaldoDbContext _context;

    public KategorieKontoMappingRepository(SaldoDbContext context)
    {
        _context = context;
    }

    public async Task<List<KategorieKontoMapping>> GetAllAsync(string? kontenrahmen = null)
    {
        var query = _context.KategorieKontoMappings
            .Include(m => m.Konto)
            .AsQueryable();
        if (!string.IsNullOrEmpty(kontenrahmen))
            query = query.Where(m => m.Kontenrahmen == kontenrahmen);
        return await query.OrderBy(m => m.ReceiptaKategorie).ToListAsync();
    }

    public async Task<KategorieKontoMapping?> GetByKategorieAsync(string kontenrahmen, string kategorieNamen)
    {
        return await _context.KategorieKontoMappings
            .Include(m => m.Konto)
            .FirstOrDefaultAsync(m => m.Kontenrahmen == kontenrahmen && m.ReceiptaKategorie == kategorieNamen);
    }

    public async Task<KategorieKontoMapping?> GetByIdAsync(Guid id)
    {
        return await _context.KategorieKontoMappings
            .Include(m => m.Konto)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task AddRangeAsync(IEnumerable<KategorieKontoMapping> mappings)
    {
        await _context.KategorieKontoMappings.AddRangeAsync(mappings);
        await _context.SaveChangesAsync();
    }

    public async Task<KategorieKontoMapping> UpdateAsync(KategorieKontoMapping mapping)
    {
        _context.KategorieKontoMappings.Update(mapping);
        await _context.SaveChangesAsync();
        return mapping;
    }

    public async Task<bool> ExistsAsync(string kontenrahmen, string kategorieNamen)
    {
        return await _context.KategorieKontoMappings
            .AnyAsync(m => m.Kontenrahmen == kontenrahmen && m.ReceiptaKategorie == kategorieNamen);
    }
}
