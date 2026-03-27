using Kuestencode.Shared.Contracts.Recepta;

namespace Kuestencode.Werkbank.Saldo.Services;

/// <summary>
/// Service für den HTTP-Zugriff auf Recepta-Daten (Belege/Ausgaben).
/// </summary>
public interface IReceptaDataService
{
    Task<List<ReceptaDocumentDto>> GetDocumentsAsync(DateOnly von, DateOnly bis);
}
