using Kuestencode.Werkbank.Host.Models;

namespace Kuestencode.Werkbank.Host.Services.Backup;

/// <summary>
/// Ein Backup-Ziel-Transport. Implementierungen erhalten <paramref name="target"/> stets mit
/// bereits entschlüsselten Zugangsdaten (siehe <see cref="BackupService"/>) und sind selbst
/// zustandslos.
/// </summary>
public interface IBackupTargetProvider
{
    Task UploadAsync(BackupTarget target, string localPath, string remoteName, CancellationToken ct);
    Task DownloadAsync(BackupTarget target, string remoteName, string localPath, CancellationToken ct = default);
    Task<List<BackupFileInfo>> ListFilesAsync(BackupTarget target, CancellationToken ct);
    Task DeleteAsync(BackupTarget target, string remoteName, CancellationToken ct);

    /// <summary>
    /// Prüft die Verbindung. <c>Error</c> enthält bei Misserfolg die konkrete Ausnahme-Message
    /// (Host nicht erreichbar, Auth fehlgeschlagen, Pfad ungültig, ...) statt sie zu verschlucken.
    /// </summary>
    Task<(bool Success, string? Error)> TestConnectionAsync(BackupTarget target, CancellationToken ct);
}
