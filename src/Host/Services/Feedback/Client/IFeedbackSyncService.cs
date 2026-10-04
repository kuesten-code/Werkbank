namespace Kuestencode.Werkbank.Host.Services.Feedback.Client;

/// <param name="Executed">false, wenn die Instanz nicht als Client konfiguriert ist.</param>
/// <param name="Error">Fehler beim Abrufen der Änderungen (Senden-Fehler stehen am jeweiligen Eintrag).</param>
public record FeedbackSyncResult(bool Executed, string? Error);

public interface IFeedbackSyncService
{
    /// <summary>
    /// Sendet fällige Outbox-Einträge (Meldungen vor Kommentaren) und ruft bei
    /// <paramref name="poll"/> = true die Änderungen seit dem letzten Abruf vom Hub ab.
    /// </summary>
    Task<FeedbackSyncResult> SyncAsync(bool poll, CancellationToken ct = default);
}
