using Kuestencode.Werkbank.Acta.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Acta.Data.Repositories;

public class ProjektStundensatzRepository : IProjektStundensatzRepository
{
    private readonly IDbContextFactory<ActaDbContext> _contextFactory;

    public ProjektStundensatzRepository(IDbContextFactory<ActaDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<ProjektStundensatz>> GetByProjektIdAsync(Guid projektId)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.ProjektStundensaetze
            .Where(s => s.ProjectId == projektId)
            .ToListAsync();
    }

    public async Task<ProjektStundensatz?> GetByProjektIdAndRolleAsync(Guid projektId, int rolleId)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.ProjektStundensaetze
            .FirstOrDefaultAsync(s => s.ProjectId == projektId && s.RolleId == rolleId);
    }

    public async Task AddAsync(ProjektStundensatz stundensatz)
    {
        await using var context = _contextFactory.CreateDbContext();
        await context.ProjektStundensaetze.AddAsync(stundensatz);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ProjektStundensatz stundensatz)
    {
        await using var context = _contextFactory.CreateDbContext();
        context.ProjektStundensaetze.Update(stundensatz);
        await context.SaveChangesAsync();
    }
}
