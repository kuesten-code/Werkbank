using Kuestencode.Werkbank.Locatio.Domain.Dtos;
using Kuestencode.Werkbank.Locatio.Domain.Entities;

namespace Kuestencode.Werkbank.Locatio.Services;

public interface IEinheitService
{
    /// <summary>Lädt die Einheit mit Gebäude samt dessen Einheiten (für die AfA-Verteilung), ohne Dateiinhalte.</summary>
    Task<Mieteinheit?> GetAsync(Guid id);

    Task<Mieteinheit> SaveAsync(Mieteinheit einheit);
    Task SetGrundrissAsync(Guid einheitId, DateiUpload datei);
    Task DeleteAsync(Guid id);
}
