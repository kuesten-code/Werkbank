using Kuestencode.Werkbank.Saldo.Domain.Dtos;

namespace Kuestencode.Werkbank.Saldo.Services;

/// <summary>
/// Aggregiert Einnahmen aus Faktura nach Zufluss-/Abflussprinzip (PaidDate).
/// </summary>
public interface IEinnahmenService
{
    Task<List<BuchungDto>> GetEinnahmenAsync(DateOnly von, DateOnly bis);
    Task<decimal> GetSummeAsync(DateOnly von, DateOnly bis);
    Task<Dictionary<string, decimal>> GetNachUstSatzAsync(DateOnly von, DateOnly bis);
}
