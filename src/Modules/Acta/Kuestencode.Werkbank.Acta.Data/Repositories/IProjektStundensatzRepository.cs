using Kuestencode.Werkbank.Acta.Domain.Entities;

namespace Kuestencode.Werkbank.Acta.Data.Repositories;

public interface IProjektStundensatzRepository
{
    Task<List<ProjektStundensatz>> GetByProjektIdAsync(Guid projektId);
    Task<ProjektStundensatz?> GetByProjektIdAndRolleAsync(Guid projektId, int rolleId);
    Task AddAsync(ProjektStundensatz stundensatz);
    Task UpdateAsync(ProjektStundensatz stundensatz);
}
