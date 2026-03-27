using Kuestencode.Werkbank.Saldo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Saldo.Data.Repositories;

public class KontoMappingOverrideRepository : IKontoMappingOverrideRepository
{
    private readonly IDbContextFactory<SaldoDbContext> _factory;

    public KontoMappingOverrideRepository(IDbContextFactory<SaldoDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<KontoMappingOverride>> GetAllAsync(string kontenrahmen)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.KontoMappingOverrides
            .Where(o => o.Kontenrahmen == kontenrahmen)
            .OrderBy(o => o.Kategorie)
            .ToListAsync();
    }

    public async Task<KontoMappingOverride?> GetByKategorieAsync(string kontenrahmen, string kategorie)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.KontoMappingOverrides
            .FirstOrDefaultAsync(o => o.Kontenrahmen == kontenrahmen && o.Kategorie == kategorie);
    }

    public async Task<KontoMappingOverride> UpsertAsync(string kontenrahmen, string kategorie, string kontoNummer)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        var existing = await ctx.KontoMappingOverrides
            .FirstOrDefaultAsync(o => o.Kontenrahmen == kontenrahmen && o.Kategorie == kategorie);

        if (existing != null)
        {
            existing.KontoNummer = kontoNummer;
            ctx.KontoMappingOverrides.Update(existing);
            await ctx.SaveChangesAsync();
            return existing;
        }

        var newOverride = new KontoMappingOverride
        {
            Id = Guid.NewGuid(),
            Kontenrahmen = kontenrahmen,
            Kategorie = kategorie,
            KontoNummer = kontoNummer
        };

        ctx.KontoMappingOverrides.Add(newOverride);
        await ctx.SaveChangesAsync();
        return newOverride;
    }

    public async Task DeleteAsync(string kontenrahmen, string kategorie)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        var existing = await ctx.KontoMappingOverrides
            .FirstOrDefaultAsync(o => o.Kontenrahmen == kontenrahmen && o.Kategorie == kategorie);

        if (existing != null)
        {
            ctx.KontoMappingOverrides.Remove(existing);
            await ctx.SaveChangesAsync();
        }
    }
}
