using Kuestencode.Werkbank.Acta.Domain.Entities;

namespace Kuestencode.Werkbank.Acta.Data.Repositories;

public interface IProjektBerechneterAufwandRepository
{
    Task<List<ProjektBerechneterAufwand>> GetByProjektIdAsync(Guid projektId);
    Task<HashSet<string>> GetBelegnummernByProjektIdAsync(Guid projektId);
    Task AddRangeAsync(IEnumerable<ProjektBerechneterAufwand> aufwaende);
}
