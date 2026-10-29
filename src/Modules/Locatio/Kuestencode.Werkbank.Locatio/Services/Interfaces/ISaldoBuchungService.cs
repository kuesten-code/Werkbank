using Kuestencode.Shared.Contracts.Locatio;

namespace Kuestencode.Werkbank.Locatio.Services;

public interface ISaldoBuchungService
{
    /// <summary>Alle an Saldo zu übergebenden Buchungen mit Zahlungsdatum im Zeitraum.</summary>
    Task<List<LocatioBuchungDto>> GetBuchungenAsync(DateOnly von, DateOnly bis);

    /// <summary>Wie <see cref="GetBuchungenAsync"/>, zusätzlich mit dem Gebäude jeder Buchung für die Kacheln.</summary>
    Task<List<(Guid GebaeudeId, LocatioBuchungDto Buchung)>> GetBuchungenMitGebaeudeAsync(DateOnly von, DateOnly bis);
}
