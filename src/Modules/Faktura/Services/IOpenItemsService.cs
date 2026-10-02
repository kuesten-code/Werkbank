using Kuestencode.Faktura.Models;

namespace Kuestencode.Faktura.Services;

public interface IOpenItemsService
{
    /// <summary>
    /// Offene Rechnungen mit Rechnungsdatum im Zeitraum (inklusive), bewertet zum heutigen Stand.
    /// </summary>
    Task<IReadOnlyList<OpenItem>> GetOpenItemsAsync(DateTime from, DateTime to);
}
