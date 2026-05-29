using Kuestencode.Werkbank.Contracta.Domain.Entities;
using Kuestencode.Werkbank.Contracta.Domain.Enums;

namespace Kuestencode.Werkbank.Contracta.Data.Repositories;

public interface IWartungsvertragRepository
{
    Task<Wartungsvertrag?> GetByIdAsync(Guid id);
    Task<Wartungsvertrag?> GetByNummerAsync(string vertragsnummer);
    Task<List<Wartungsvertrag>> GetAllAsync();
    Task<List<Wartungsvertrag>> GetByKundeAsync(int kundeId);
    Task<List<Wartungsvertrag>> GetByStatusAsync(VertragStatus status);
    Task<List<Wartungsvertrag>> GetAktiveAsync();
    Task AddAsync(Wartungsvertrag vertrag);
    Task UpdateAsync(Wartungsvertrag vertrag);
    Task DeleteAsync(Guid id);
    Task<string> GenerateVertragsnummerAsync();
}
