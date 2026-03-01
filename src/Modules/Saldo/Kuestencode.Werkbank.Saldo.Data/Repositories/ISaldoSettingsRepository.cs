using Kuestencode.Werkbank.Saldo.Domain.Entities;

namespace Kuestencode.Werkbank.Saldo.Data.Repositories;

/// <summary>
/// Repository für Saldo-Einstellungen (genau ein Datensatz pro Mandant).
/// </summary>
public interface ISaldoSettingsRepository
{
    Task<SaldoSettings?> GetAsync();
    Task<SaldoSettings> CreateAsync(SaldoSettings settings);
    Task<SaldoSettings> UpdateAsync(SaldoSettings settings);
}
