using Kuestencode.Werkbank.Locatio.Domain.Dtos;

namespace Kuestencode.Werkbank.Locatio.Services;

public interface IAfaService
{
    /// <summary>AfA-Vorschläge eines Jahres für alle Einheiten mit AfA-Basis.</summary>
    Task<List<AfaVorschlagDto>> GetVorschlaegeAsync(int jahr);

    /// <summary>Setzt die AfA einer Einheit für ein Jahr als Kostenposten an.</summary>
    Task BucheAsync(Guid einheitId, int jahr);
}
