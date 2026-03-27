using Kuestencode.Werkbank.Saldo.Domain.Dtos;

namespace Kuestencode.Werkbank.Saldo.Services;

public interface IKontoService
{
    Task<List<KontoDto>> GetKontenAsync(string? kontenrahmen = null);
    Task<List<KategorieKontoMappingDto>> GetMappingsAsync(string? kontenrahmen = null);
    Task<KategorieKontoMappingDto?> UpdateMappingAsync(Guid id, UpdateKategorieKontoMappingDto dto);
}
