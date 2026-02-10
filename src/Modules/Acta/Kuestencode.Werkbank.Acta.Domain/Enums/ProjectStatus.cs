namespace Kuestencode.Werkbank.Acta.Domain.Enums;

/// <summary>
/// Status eines Projekts im Lebenszyklus.
/// </summary>
public enum ProjectStatus
{
    /// <summary>
    /// Projekt ist in Planung und noch nicht aktiv.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Projekt ist aktiv und in Bearbeitung.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Projekt ist pausiert.
    /// </summary>
    Paused = 2,

    /// <summary>
    /// Projekt wurde abgeschlossen.
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Projekt wurde archiviert.
    /// </summary>
    Archived = 4
}
