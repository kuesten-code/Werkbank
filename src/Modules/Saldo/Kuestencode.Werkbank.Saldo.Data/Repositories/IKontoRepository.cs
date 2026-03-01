using Kuestencode.Werkbank.Saldo.Domain.Entities;

namespace Kuestencode.Werkbank.Saldo.Data.Repositories;

/// <summary>
/// Repository für den Kontenstamm.
/// </summary>
public interface IKontoRepository
{
    Task<List<Konto>> GetAllAsync(string? kontenrahmen = null);
    Task<Konto?> GetByNummerAsync(string kontenrahmen, string kontoNummer);
    Task<List<Konto>> GetByKontenrahmenAsync(string kontenrahmen);
    Task AddRangeAsync(IEnumerable<Konto> konten);
    Task<bool> ExistsAsync(string kontenrahmen, string kontoNummer);
}
