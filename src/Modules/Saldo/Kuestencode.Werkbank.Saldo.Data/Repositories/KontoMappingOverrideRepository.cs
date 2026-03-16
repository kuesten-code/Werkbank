using Kuestencode.Werkbank.Saldo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Saldo.Data.Repositories;

public class KontoMappingOverrideRepository : IKontoMappingOverrideRepository
{
    private readonly SaldoDbContext _context;

    public KontoMappingOverrideRepository(SaldoDbContext context)
    {
        _context = context;
    }

    public async Task<List<KontoMappingOverride>> GetAllAsync(string kontenrahmen)
    {
        return await _context.KontoMappingOverrides

            .Where(o => o.Kontenrahmen == kontenrahmen)
            .OrderBy(o => o.Kategorie)
            .ToListAsync();
    }

    public async Task<KontoMappingOverride?> GetByKategorieAsync(string kontenrahmen, string kategorie)
    {
        return await _context.KontoMappingOverrides

            .FirstOrDefaultAsync(o => o.Kontenrahmen == kontenrahmen && o.Kategorie == kategorie);
    }

    public async Task<KontoMappingOverride> UpsertAsync(string kontenrahmen, string kategorie, string kontoNummer)
    {
        var existing = await _context.KontoMappingOverrides
            .FirstOrDefaultAsync(o => o.Kontenrahmen == kontenrahmen && o.Kategorie == kategorie);

        if (existing != null)
        {
            existing.KontoNummer = kontoNummer;
            _context.KontoMappingOverrides.Update(existing);
            await _context.SaveChangesAsync();

            return await _context.KontoMappingOverrides
    
                .FirstAsync(o => o.Id == existing.Id);
        }

        var newOverride = new KontoMappingOverride
        {
            Id = Guid.NewGuid(),
            Kontenrahmen = kontenrahmen,
            Kategorie = kategorie,
            KontoNummer = kontoNummer
        };

        _context.KontoMappingOverrides.Add(newOverride);
        await _context.SaveChangesAsync();

        return await _context.KontoMappingOverrides

            .FirstAsync(o => o.Id == newOverride.Id);
    }

    public async Task DeleteAsync(string kontenrahmen, string kategorie)
    {
        var existing = await _context.KontoMappingOverrides
            .FirstOrDefaultAsync(o => o.Kontenrahmen == kontenrahmen && o.Kategorie == kategorie);

        if (existing != null)
        {
            _context.KontoMappingOverrides.Remove(existing);
            await _context.SaveChangesAsync();
        }
    }
}
