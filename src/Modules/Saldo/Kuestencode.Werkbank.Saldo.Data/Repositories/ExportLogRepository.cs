using Kuestencode.Werkbank.Saldo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Saldo.Data.Repositories;

public class ExportLogRepository : IExportLogRepository
{
    private readonly SaldoDbContext _context;

    public ExportLogRepository(SaldoDbContext context)
    {
        _context = context;
    }

    public async Task<List<ExportLog>> GetAllAsync()
    {
        return await _context.ExportLogs
            .OrderByDescending(e => e.ExportedAt)
            .ToListAsync();
    }

    public async Task<ExportLog> AddAsync(ExportLog log)
    {
        log.Id = Guid.NewGuid();
        await _context.ExportLogs.AddAsync(log);
        await _context.SaveChangesAsync();
        return log;
    }
}
