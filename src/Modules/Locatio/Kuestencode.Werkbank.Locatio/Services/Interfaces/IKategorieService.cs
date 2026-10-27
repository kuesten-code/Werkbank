using Kuestencode.Werkbank.Locatio.Domain.Entities;

namespace Kuestencode.Werkbank.Locatio.Services;

public interface IKategorieService
{
    Task<List<Kostenkategorie>> GetAllAsync();

    /// <summary>Kategorien, die als Kostenposten erfasst werden können.</summary>
    Task<List<Kostenkategorie>> GetAusgabekategorienAsync();

    Task<Kostenkategorie> SaveAsync(Kostenkategorie kategorie);
    Task DeleteAsync(Guid id);
}
