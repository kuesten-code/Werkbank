using Kuestencode.Werkbank.Locatio.Domain.Entities;

namespace Kuestencode.Werkbank.Locatio.Services;

public interface IMieteingangService
{
    Task<List<Mieteingang>> GetAsync(int jahr, Guid? einheitId = null);
    Task<Mieteingang> SaveAsync(Mieteingang eingang);
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Erfasst für alle vermieteten eigenen Einheiten die vereinbarte Miete eines Monats.
    /// Einheiten, für die der Monat schon erfasst ist oder die gesperrt sind, werden übersprungen.
    /// </summary>
    Task<int> ErfasseMonatAsync(DateOnly monat, DateOnly zahlungsdatum);
}
