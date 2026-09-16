namespace Kuestencode.Werkbank.Host.Services.Backup;

/// <summary>
/// Signalisiert dem <see cref="BackupSchedulerService"/>, dass sich die Backup-Einstellungen
/// (insbesondere der Zeitplan) geändert haben, damit er seine aktuelle Wartezeit sofort
/// abbricht und neu berechnet - statt bis zum Ende des (u.U. stunden- oder tagelangen) zuvor
/// berechneten Delays zu schlafen und die Änderung erst danach zu bemerken.
/// </summary>
public interface IBackupScheduleChangeSignal
{
    CancellationToken Token { get; }
    void SignalChanged();
}
