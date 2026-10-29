using Kuestencode.Werkbank.Locatio.Domain.Entities;

namespace Kuestencode.Werkbank.Locatio.Services;

public interface IDarlehenService
{
    /// <summary>Alle Darlehen mit Gebäude und Zahlungen.</summary>
    Task<List<Darlehen>> GetAllAsync();

    Task<Darlehen> SaveAsync(Darlehen darlehen);
    Task DeleteAsync(Guid id);

    Task<Darlehenszahlung> SaveZahlungAsync(Darlehenszahlung zahlung);
    Task DeleteZahlungAsync(Guid id);
}
