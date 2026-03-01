using Kuestencode.Werkbank.Saldo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Saldo.Data.Repositories;

public class KontoRepository : IKontoRepository
{
    private readonly SaldoDbContext _context;

    public KontoRepository(SaldoDbContext context)
    {
        _context = context;
    }

    public async Task<List<Konto>> GetAllAsync(string? kontenrahmen = null)
    {
        var query = _context.Konten.AsQueryable();
        if (!string.IsNullOrEmpty(kontenrahmen))
            query = query.Where(k => k.Kontenrahmen == kontenrahmen);
        return await query.OrderBy(k => k.KontoNummer).ToListAsync();
    }

    public async Task<Konto?> GetByNummerAsync(string kontenrahmen, string kontoNummer)
    {
        return await _context.Konten
            .FirstOrDefaultAsync(k => k.Kontenrahmen == kontenrahmen && k.KontoNummer == kontoNummer);
    }

    public async Task<List<Konto>> GetByKontenrahmenAsync(string kontenrahmen)
    {
        return await _context.Konten
            .Where(k => k.Kontenrahmen == kontenrahmen)
            .OrderBy(k => k.KontoNummer)
            .ToListAsync();
    }

    public async Task AddRangeAsync(IEnumerable<Konto> konten)
    {
        await _context.Konten.AddRangeAsync(konten);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(string kontenrahmen, string kontoNummer)
    {
        return await _context.Konten
            .AnyAsync(k => k.Kontenrahmen == kontenrahmen && k.KontoNummer == kontoNummer);
    }
}
