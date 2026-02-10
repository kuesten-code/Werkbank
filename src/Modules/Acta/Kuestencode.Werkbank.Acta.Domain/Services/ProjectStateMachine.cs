using Kuestencode.Werkbank.Acta.Domain.Enums;

namespace Kuestencode.Werkbank.Acta.Domain.Services;

/// <summary>
/// Statische State Machine für Projektstatus-Übergänge.
///
/// Erlaubte Übergänge:
/// - Draft → Active (Freigeben)
/// - Active → Paused (Pausieren)
/// - Paused → Active (Fortsetzen)
/// - Active → Completed (Abschließen)
/// - Completed → Active (Reaktivieren)
/// - Completed → Archived (Archivieren)
///
/// Terminal-Status: Archived
/// </summary>
public static class ProjectStateMachine
{
    /// <summary>
    /// Definiert alle erlaubten Statusübergänge mit ihren Aktionsnamen.
    /// </summary>
    private static readonly Dictionary<(ProjectStatus From, ProjectStatus To), string> AllowedTransitions = new()
    {
        { (ProjectStatus.Draft, ProjectStatus.Active), "Freigeben" },
        { (ProjectStatus.Active, ProjectStatus.Paused), "Pausieren" },
        { (ProjectStatus.Paused, ProjectStatus.Active), "Fortsetzen" },
        { (ProjectStatus.Active, ProjectStatus.Completed), "Abschließen" },
        { (ProjectStatus.Completed, ProjectStatus.Active), "Reaktivieren" },
        { (ProjectStatus.Completed, ProjectStatus.Archived), "Archivieren" }
    };

    /// <summary>
    /// Prüft, ob ein Statusübergang erlaubt ist.
    /// </summary>
    /// <param name="from">Aktueller Status</param>
    /// <param name="to">Zielstatus</param>
    /// <returns>True, wenn der Übergang erlaubt ist</returns>
    public static bool CanTransition(ProjectStatus from, ProjectStatus to)
    {
        return AllowedTransitions.ContainsKey((from, to));
    }

    /// <summary>
    /// Gibt alle verfügbaren Übergänge für einen Status zurück.
    /// </summary>
    /// <param name="currentStatus">Aktueller Status</param>
    /// <returns>Liste von (Zielstatus, Aktionsname)</returns>
    public static IReadOnlyList<(ProjectStatus TargetStatus, string ActionName)> GetAvailableTransitions(ProjectStatus currentStatus)
    {
        return AllowedTransitions
            .Where(kvp => kvp.Key.From == currentStatus)
            .Select(kvp => (kvp.Key.To, kvp.Value))
            .ToList();
    }

    /// <summary>
    /// Gibt den Aktionsnamen für einen Statusübergang zurück.
    /// </summary>
    /// <param name="from">Aktueller Status</param>
    /// <param name="to">Zielstatus</param>
    /// <returns>Aktionsname oder null, wenn Übergang nicht erlaubt</returns>
    public static string? GetActionName(ProjectStatus from, ProjectStatus to)
    {
        return AllowedTransitions.TryGetValue((from, to), out var actionName) ? actionName : null;
    }

    /// <summary>
    /// Prüft, ob ein Status terminal ist (keine weiteren Übergänge möglich).
    /// </summary>
    /// <param name="status">Zu prüfender Status</param>
    /// <returns>True, wenn der Status terminal ist</returns>
    public static bool IsTerminal(ProjectStatus status)
    {
        return status == ProjectStatus.Archived;
    }

    /// <summary>
    /// Prüft, ob ein Projekt im gegebenen Status bearbeitet werden kann.
    /// </summary>
    /// <param name="status">Aktueller Status</param>
    /// <returns>True, wenn das Projekt bearbeitet werden kann</returns>
    public static bool IsEditable(ProjectStatus status)
    {
        return status is ProjectStatus.Draft or ProjectStatus.Active or ProjectStatus.Paused;
    }
}
