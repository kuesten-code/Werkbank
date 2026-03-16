using Kuestencode.Werkbank.Saldo.Domain.Entities;

namespace Kuestencode.Werkbank.Saldo.Data.Repositories;

/// <summary>
/// Repository für benutzerdefinierte Konto-Mapping-Overrides.
/// </summary>
public interface IKontoMappingOverrideRepository
{
    Task<List<KontoMappingOverride>> GetAllAsync(string kontenrahmen);
    Task<KontoMappingOverride?> GetByKategorieAsync(string kontenrahmen, string kategorie);
    Task<KontoMappingOverride> UpsertAsync(string kontenrahmen, string kategorie, string kontoNummer);
    Task DeleteAsync(string kontenrahmen, string kategorie);
}
