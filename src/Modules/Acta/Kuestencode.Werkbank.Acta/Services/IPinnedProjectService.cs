using Kuestencode.Werkbank.Acta.Domain.Entities;

namespace Kuestencode.Werkbank.Acta.Services;

/// <summary>
/// Verwaltet die pro Nutzer angehefteten Projekt-Kacheln auf dem Acta-Dashboard.
/// </summary>
public interface IPinnedProjectService
{
    /// <summary>
    /// Lädt die vom aktuellen Nutzer angehefteten Projekte.
    /// </summary>
    Task<List<Project>> GetPinnedProjectsAsync();

    /// <summary>
    /// Lädt aktive Projekte, die der aktuelle Nutzer noch nicht angeheftet hat.
    /// </summary>
    Task<List<Project>> GetPinnableActiveProjectsAsync();

    /// <summary>
    /// Heftet ein Projekt für den aktuellen Nutzer an. Bereits angeheftete Projekte bleiben unverändert (kein Fehler).
    /// </summary>
    Task PinAsync(Guid projectId);

    /// <summary>
    /// Entfernt ein Projekt aus der Kachel-Ansicht des aktuellen Nutzers. Nicht angeheftete Projekte lösen keinen Fehler aus.
    /// </summary>
    Task UnpinAsync(Guid projectId);
}
