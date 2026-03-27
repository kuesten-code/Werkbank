using Kuestencode.Werkbank.Saldo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Saldo.Data.Repositories;

public class SaldoSettingsRepository : ISaldoSettingsRepository
{
    private readonly IDbContextFactory<SaldoDbContext> _factory;

    public SaldoSettingsRepository(IDbContextFactory<SaldoDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<SaldoSettings?> GetAsync()
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.SaldoSettings.FirstOrDefaultAsync();
    }

    public async Task<SaldoSettings> CreateAsync(SaldoSettings settings)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        settings.Id = Guid.NewGuid();
        await ctx.SaldoSettings.AddAsync(settings);
        await ctx.SaveChangesAsync();
        return settings;
    }

    public async Task<SaldoSettings> UpdateAsync(SaldoSettings settings)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        ctx.SaldoSettings.Update(settings);
        await ctx.SaveChangesAsync();
        return settings;
    }
}
