using Kuestencode.Werkbank.Saldo.Domain.Dtos;

namespace Kuestencode.Werkbank.Saldo.Services;

/// <summary>
/// Aggregiert Einnahmen und Ausgaben zu einer Saldoübersicht.
/// </summary>
public interface ISaldoAggregationService
{
    Task<SaldoUebersichtDto> GetUebersichtAsync(DateOnly von, DateOnly bis);
    Task<List<BuchungDto>> GetAlleBuchungenAsync(DateOnly von, DateOnly bis);
    Task<UstUebersichtDto> GetUstUebersichtAsync(DateOnly von, DateOnly bis);
}
