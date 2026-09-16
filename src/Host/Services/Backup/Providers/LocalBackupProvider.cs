using Kuestencode.Werkbank.Host.Models;
using Kuestencode.Werkbank.Host.Services;

namespace Kuestencode.Werkbank.Host.Services.Backup.Providers;

public class LocalBackupProvider : IBackupTargetProvider
{
    public Task UploadAsync(BackupTarget target, string localPath, string remoteName, CancellationToken ct)
    {
        Directory.CreateDirectory(target.Path!);
        var destPath = Path.Combine(target.Path!, remoteName);
        File.Copy(localPath, destPath, overwrite: true);
        return Task.CompletedTask;
    }

    public Task DownloadAsync(BackupTarget target, string remoteName, string localPath, CancellationToken ct = default)
    {
        var sourcePath = Path.Combine(target.Path!, remoteName);
        File.Copy(sourcePath, localPath, overwrite: true);
        return Task.CompletedTask;
    }

    public Task<List<BackupFileInfo>> ListFilesAsync(BackupTarget target, CancellationToken ct)
    {
        if (!Directory.Exists(target.Path))
            return Task.FromResult(new List<BackupFileInfo>());

        var files = Directory.GetFiles(target.Path!)
            .Select(f => new FileInfo(f))
            .Where(f => BackupFileNaming.IsBackupFile(f.Name))
            .Select(f =>
            {
                var date = BackupFileNaming.ParseDateFromFileName(f.Name);
                return new BackupFileInfo(f.Name, date, f.Length, BackupFileNaming.DetermineBackupType(date));
            })
            .ToList();

        return Task.FromResult(files);
    }

    public Task DeleteAsync(BackupTarget target, string remoteName, CancellationToken ct)
    {
        var path = Path.Combine(target.Path!, remoteName);
        File.Delete(path);
        return Task.CompletedTask;
    }

    public Task<(bool Success, string? Error)> TestConnectionAsync(BackupTarget target, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(target.Path))
            return Task.FromResult<(bool, string?)>((false, "Kein Pfad angegeben"));

        try
        {
            Directory.CreateDirectory(target.Path);
            return Task.FromResult<(bool, string?)>((true, null));
        }
        catch (Exception ex)
        {
            return Task.FromResult<(bool, string?)>((false, ExceptionHelper.Describe(ex)));
        }
    }
}
