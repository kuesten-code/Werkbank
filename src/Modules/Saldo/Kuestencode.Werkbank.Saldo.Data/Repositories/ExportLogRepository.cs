using Kuestencode.Werkbank.Saldo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Saldo.Data.Repositories;

public class ExportLogRepository : IExportLogRepository
{
    private readonly IDbContextFactory<SaldoDbContext> _factory;

    public ExportLogRepository(IDbContextFactory<SaldoDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<ExportLog>> GetAllAsync()
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.ExportLogs
            .OrderByDescending(e => e.ExportedAt)
            .ToListAsync();
    }

    public async Task<ExportLog> AddAsync(ExportLog log)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        log.Id = Guid.NewGuid();
        await ctx.ExportLogs.AddAsync(log);
        await ctx.SaveChangesAsync();
        return log;
    }
}
