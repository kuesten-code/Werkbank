using Kuestencode.Shared.Contracts.Locatio;

namespace Kuestencode.Werkbank.Saldo.Services;

/// <summary>
/// HTTP-Zugriff auf die Buchungen der Vermietungsverwaltung (Locatio).
/// </summary>
public interface ILocatioDataService
{
    Task<List<LocatioBuchungDto>> GetBuchungenAsync(DateOnly von, DateOnly bis);
}
