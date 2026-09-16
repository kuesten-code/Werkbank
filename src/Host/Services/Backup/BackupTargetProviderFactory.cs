using Kuestencode.Werkbank.Host.Models;
using Kuestencode.Werkbank.Host.Services.Backup.Providers;

namespace Kuestencode.Werkbank.Host.Services.Backup;

public class BackupTargetProviderFactory : IBackupTargetProviderFactory
{
    public IBackupTargetProvider GetProvider(BackupTargetType type) => type switch
    {
        BackupTargetType.Local => new LocalBackupProvider(),
        BackupTargetType.Sftp => new SftpBackupProvider(),
        BackupTargetType.S3 => new S3BackupProvider(),
        BackupTargetType.WebDav => new WebDavBackupProvider(),
        _ => throw new NotSupportedException($"Backup-Ziel {type} nicht unterstützt")
    };
}
