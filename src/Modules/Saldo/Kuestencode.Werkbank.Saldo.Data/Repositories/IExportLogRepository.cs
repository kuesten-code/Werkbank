using Kuestencode.Werkbank.Saldo.Domain.Entities;

namespace Kuestencode.Werkbank.Saldo.Data.Repositories;

/// <summary>
/// Repository für Export-Protokolle.
/// </summary>
public interface IExportLogRepository
{
    Task<List<ExportLog>> GetAllAsync();
    Task<ExportLog> AddAsync(ExportLog log);
}
