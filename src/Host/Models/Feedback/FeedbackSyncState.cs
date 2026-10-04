namespace Kuestencode.Werkbank.Host.Models.Feedback;

/// <summary>Outbox-Zustand eines lokal angelegten Eintrags (Rolle Client).</summary>
public enum FeedbackSyncState
{
    Ausstehend = 0,
    Gesendet = 1,
    Fehlgeschlagen = 2
}
