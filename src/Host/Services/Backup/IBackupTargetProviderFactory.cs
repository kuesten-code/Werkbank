using Kuestencode.Werkbank.Host.Models;

namespace Kuestencode.Werkbank.Host.Services.Backup;

public interface IBackupTargetProviderFactory
{
    IBackupTargetProvider GetProvider(BackupTargetType type);
}
