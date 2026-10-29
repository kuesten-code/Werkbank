using Kuestencode.Werkbank.Locatio.Domain.Dtos;
using Kuestencode.Werkbank.Locatio.Domain.Entities;

namespace Kuestencode.Werkbank.Locatio.Services;

public interface IGebaeudeService
{
    Task<List<GebaeudeKachelDto>> GetKachelnAsync(int jahr);
    Task<List<Gebaeude>> GetAllAsync();

    /// <summary>Lädt das Gebäude mit seinen Einheiten.</summary>
    Task<Gebaeude?> GetAsync(Guid id);

    Task<Gebaeude> SaveAsync(Gebaeude gebaeude);
    Task DeleteAsync(Guid id);
}
