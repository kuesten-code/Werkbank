using Kuestencode.Werkbank.Locatio.Domain.Dtos;

namespace Kuestencode.Werkbank.Locatio.Services;

/// <summary>
/// Veräußerung in zwei Schritten: <c>Pruefe…</c> liefert die Warnungen zur Bestätigung,
/// <c>Veraeussere…</c> führt den Verkauf aus und liefert dieselben Warnungen als Protokoll.
/// </summary>
public interface IVeraeusserungService
{
    Task<List<string>> PruefeEinheitAsync(Guid einheitId, DateOnly datum);
    Task<List<string>> VeraeussereEinheitAsync(Guid einheitId, VeraeusserungDaten daten);

    Task<List<string>> PruefeGebaeudeAsync(Guid gebaeudeId, DateOnly datum);
    Task<List<string>> VeraeussereGebaeudeAsync(Guid gebaeudeId, VeraeusserungDaten daten);
}
