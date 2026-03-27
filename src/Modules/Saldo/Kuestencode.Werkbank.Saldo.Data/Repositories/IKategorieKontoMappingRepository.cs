using Kuestencode.Werkbank.Saldo.Domain.Entities;

namespace Kuestencode.Werkbank.Saldo.Data.Repositories;

/// <summary>
/// Repository für Kategorie-Konto-Mappings.
/// </summary>
public interface IKategorieKontoMappingRepository
{
    Task<List<KategorieKontoMapping>> GetAllAsync(string? kontenrahmen = null);
    Task<KategorieKontoMapping?> GetByKategorieAsync(string kontenrahmen, string kategorieNamen);
    Task<KategorieKontoMapping?> GetByIdAsync(Guid id);
    Task AddRangeAsync(IEnumerable<KategorieKontoMapping> mappings);
    Task<KategorieKontoMapping> UpdateAsync(KategorieKontoMapping mapping);
    Task<bool> ExistsAsync(string kontenrahmen, string kategorieNamen);
}
