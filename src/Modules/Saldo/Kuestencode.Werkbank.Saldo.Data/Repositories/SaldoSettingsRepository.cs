using Kuestencode.Werkbank.Saldo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Saldo.Data.Repositories;

public class SaldoSettingsRepository : ISaldoSettingsRepository
{
    private readonly SaldoDbContext _context;

    public SaldoSettingsRepository(SaldoDbContext context)
    {
        _context = context;
    }

    public async Task<SaldoSettings?> GetAsync()
    {
        return await _context.SaldoSettings.FirstOrDefaultAsync();
    }

    public async Task<SaldoSettings> CreateAsync(SaldoSettings settings)
    {
        settings.Id = Guid.NewGuid();
        await _context.SaldoSettings.AddAsync(settings);
        await _context.SaveChangesAsync();
        return settings;
    }

    public async Task<SaldoSettings> UpdateAsync(SaldoSettings settings)
    {
        _context.SaldoSettings.Update(settings);
        await _context.SaveChangesAsync();
        return settings;
    }
}
