using Kuestencode.Werkbank.Saldo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Saldo.Data.Repositories;

public class KontoRepository : IKontoRepository
{
    private readonly IDbContextFactory<SaldoDbContext> _factory;

    public KontoRepository(IDbContextFactory<SaldoDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<Konto>> GetAllAsync(string? kontenrahmen = null)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        var query = ctx.Konten.AsQueryable();
        if (!string.IsNullOrEmpty(kontenrahmen))
            query = query.Where(k => k.Kontenrahmen == kontenrahmen);
        return await query.OrderBy(k => k.KontoNummer).ToListAsync();
    }

    public async Task<Konto?> GetByNummerAsync(string kontenrahmen, string kontoNummer)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.Konten
            .FirstOrDefaultAsync(k => k.Kontenrahmen == kontenrahmen && k.KontoNummer == kontoNummer);
    }

    public async Task<List<Konto>> GetByKontenrahmenAsync(string kontenrahmen)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.Konten
            .Where(k => k.Kontenrahmen == kontenrahmen)
            .OrderBy(k => k.KontoNummer)
            .ToListAsync();
    }

    public async Task AddRangeAsync(IEnumerable<Konto> konten)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        await ctx.Konten.AddRangeAsync(konten);
        await ctx.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(string kontenrahmen, string kontoNummer)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.Konten
            .AnyAsync(k => k.Kontenrahmen == kontenrahmen && k.KontoNummer == kontoNummer);
    }
}
