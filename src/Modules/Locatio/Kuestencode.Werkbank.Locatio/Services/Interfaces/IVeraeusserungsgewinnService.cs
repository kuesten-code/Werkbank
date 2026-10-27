using Kuestencode.Werkbank.Locatio.Domain.Dtos;

namespace Kuestencode.Werkbank.Locatio.Services;

/// <summary>
/// Überschlag des Veräußerungsgewinns als Orientierung für den Steuerberater. Bucht nichts in Saldo.
/// </summary>
public interface IVeraeusserungsgewinnService
{
    Task<VeraeusserungsgewinnDto?> FuerEinheitAsync(Guid einheitId);
    Task<VeraeusserungsgewinnDto?> FuerGebaeudeAsync(Guid gebaeudeId);
}
