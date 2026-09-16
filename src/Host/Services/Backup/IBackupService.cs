using Kuestencode.Werkbank.Host.Models;

namespace Kuestencode.Werkbank.Host.Services.Backup;

public interface IBackupService
{
    Task<BackupResult> RunBackupAsync(bool isManual = false, CancellationToken ct = default);

    Task<BackupSettings> GetSettingsAsync();
    Task UpdateSettingsAsync(BackupSettings settings);

    Task<List<BackupTarget>> GetTargetsAsync();
    Task<BackupTarget> AddTargetAsync(BackupTarget target);
    Task UpdateTargetAsync(BackupTarget target);
    Task DeleteTargetAsync(int targetId);
    Task<(bool Success, string? Error)> TestTargetConnectionAsync(int targetId);

    Task<List<BackupHistory>> GetHistoryAsync(int? targetId = null, int limit = 50);
    Task<List<BackupFileInfo>> ListBackupsOnTargetAsync(int targetId);

    /// <summary>
    /// Lädt eine Backup-Datei vom Ziel in einen lokalen Stream (für den manuellen Download
    /// über die UI). Der Stream löscht die temporäre Datei automatisch beim Schließen.
    /// </summary>
    Task<Stream> OpenBackupFileForDownloadAsync(int targetId, string fileName);

    /// <summary>
    /// Löscht eine einzelne Backup-Datei vom Ziel sowie die zugehörigen Historien-Einträge.
    /// </summary>
    Task DeleteBackupFileAsync(int targetId, string fileName);

    /// <summary>
    /// Stellt ein Backup wieder her. Die Bestätigung (Admin-Passwort, "RESTORE" tippen)
    /// erfolgt in der UI, bevor dieser Aufruf ausgelöst wird.
    /// </summary>
    Task<RestoreResult> RestoreAsync(int targetId, string fileName);

    Task<BackupStatusInfo> GetStatusAsync();
}
