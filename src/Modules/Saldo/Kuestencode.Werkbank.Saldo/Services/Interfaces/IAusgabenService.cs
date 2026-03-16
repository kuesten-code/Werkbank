using Kuestencode.Werkbank.Saldo.Domain.Dtos;

namespace Kuestencode.Werkbank.Saldo.Services;

/// <summary>
/// Aggregiert Ausgaben aus Recepta nach Zufluss-/Abflussprinzip (PaidDate).
/// </summary>
public interface IAusgabenService
{
    Task<List<BuchungDto>> GetAusgabenAsync(DateOnly von, DateOnly bis);
    Task<decimal> GetSummeAsync(DateOnly von, DateOnly bis);
    Task<Dictionary<string, decimal>> GetNachKategorieAsync(DateOnly von, DateOnly bis);
}
