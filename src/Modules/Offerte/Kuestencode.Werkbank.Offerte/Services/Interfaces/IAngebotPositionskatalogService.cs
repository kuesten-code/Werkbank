using Kuestencode.Werkbank.Offerte.Domain.Entities;

namespace Kuestencode.Werkbank.Offerte.Services;

/// <summary>
/// Übergibt die Positionen eines gespeicherten Angebots an den gemeinsamen Positionskatalog.
/// </summary>
public interface IAngebotPositionskatalogService
{
    Task MerkenAsync(Angebot angebot);
}
