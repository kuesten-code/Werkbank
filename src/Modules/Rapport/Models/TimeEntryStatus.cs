namespace Kuestencode.Rapport.Models;

/// <summary>
/// Status for a tracked time entry.
/// </summary>
public enum TimeEntryStatus
{
    /// <summary>
    /// Timer is currently running.
    /// </summary>
    Running,

    /// <summary>
    /// Timer has been stopped.
    /// </summary>
    Stopped,

    /// <summary>
    /// Entry was created manually.
    /// </summary>
    Manual
}
