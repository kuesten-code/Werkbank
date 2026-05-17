using Kuestencode.Werkbank.Acta.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Acta.Data.Repositories;

public class ProjektBerechneterAufwandRepository : IProjektBerechneterAufwandRepository
{
    private readonly IDbContextFactory<ActaDbContext> _contextFactory;

    public ProjektBerechneterAufwandRepository(IDbContextFactory<ActaDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<ProjektBerechneterAufwand>> GetByProjektIdAsync(Guid projektId)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.ProjektBerechneteAufwaende
            .Where(a => a.ProjectId == projektId)
            .OrderBy(a => a.BerechnedAt)
            .ToListAsync();
    }

    public async Task<HashSet<string>> GetBelegnummernByProjektIdAsync(Guid projektId)
    {
        await using var context = _contextFactory.CreateDbContext();
        var nummern = await context.ProjektBerechneteAufwaende
            .Where(a => a.ProjectId == projektId)
            .Select(a => a.Belegnummer)
            .ToListAsync();
        return nummern.ToHashSet();
    }

    public async Task AddRangeAsync(IEnumerable<ProjektBerechneterAufwand> aufwaende)
    {
        await using var context = _contextFactory.CreateDbContext();
        await context.ProjektBerechneteAufwaende.AddRangeAsync(aufwaende);
        await context.SaveChangesAsync();
    }
}
