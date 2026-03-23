using Kuestencode.Werkbank.Saldo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Saldo.Data.Repositories;

public class KategorieKontoMappingRepository : IKategorieKontoMappingRepository
{
    private readonly IDbContextFactory<SaldoDbContext> _factory;

    public KategorieKontoMappingRepository(IDbContextFactory<SaldoDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<KategorieKontoMapping>> GetAllAsync(string? kontenrahmen = null)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        var query = ctx.KategorieKontoMappings
            .Include(m => m.Konto)
            .AsQueryable();
        if (!string.IsNullOrEmpty(kontenrahmen))
            query = query.Where(m => m.Kontenrahmen == kontenrahmen);
        return await query.OrderBy(m => m.ReceiptaKategorie).ToListAsync();
    }

    public async Task<KategorieKontoMapping?> GetByKategorieAsync(string kontenrahmen, string kategorieNamen)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.KategorieKontoMappings
            .Include(m => m.Konto)
            .FirstOrDefaultAsync(m => m.Kontenrahmen == kontenrahmen && m.ReceiptaKategorie == kategorieNamen);
    }

    public async Task<KategorieKontoMapping?> GetByIdAsync(Guid id)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.KategorieKontoMappings
            .Include(m => m.Konto)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task AddRangeAsync(IEnumerable<KategorieKontoMapping> mappings)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        await ctx.KategorieKontoMappings.AddRangeAsync(mappings);
        await ctx.SaveChangesAsync();
    }

    public async Task<KategorieKontoMapping> UpdateAsync(KategorieKontoMapping mapping)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        ctx.KategorieKontoMappings.Update(mapping);
        await ctx.SaveChangesAsync();
        return mapping;
    }

    public async Task<bool> ExistsAsync(string kontenrahmen, string kategorieNamen)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.KategorieKontoMappings
            .AnyAsync(m => m.Kontenrahmen == kontenrahmen && m.ReceiptaKategorie == kategorieNamen);
    }
}
