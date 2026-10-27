using Kuestencode.Werkbank.Locatio.Domain.Dtos;
using Kuestencode.Werkbank.Locatio.Domain.Entities;

namespace Kuestencode.Werkbank.Locatio.Services;

public interface INebenkostenabrechnungService
{
    Task<List<Nebenkostenabrechnung>> GetByEinheitAsync(Guid einheitId);
    Task<Nebenkostenabrechnung> SaveAsync(Nebenkostenabrechnung abrechnung, DateiUpload? pdf = null);
    Task DeleteAsync(Guid id);
}
