using Kuestencode.Werkbank.Host.Models;
using Kuestencode.Werkbank.Host.Services;
using Renci.SshNet;

namespace Kuestencode.Werkbank.Host.Services.Backup.Providers;

public class SftpBackupProvider : IBackupTargetProvider
{
    public Task UploadAsync(BackupTarget target, string localPath, string remoteName, CancellationToken ct)
    {
        using var client = CreateClient(target);
        client.Connect();

        var remotePath = CombinePath(target.Path, remoteName);
        using var fileStream = File.OpenRead(localPath);
        client.UploadFile(fileStream, remotePath);

        return Task.CompletedTask;
    }

    public Task DownloadAsync(BackupTarget target, string remoteName, string localPath, CancellationToken ct = default)
    {
        using var client = CreateClient(target);
        client.Connect();

        var remotePath = CombinePath(target.Path, remoteName);
        using var fileStream = File.Create(localPath);
        client.DownloadFile(remotePath, fileStream);

        return Task.CompletedTask;
    }

    public Task<List<BackupFileInfo>> ListFilesAsync(BackupTarget target, CancellationToken ct)
    {
        using var client = CreateClient(target);
        client.Connect();

        var files = client.ListDirectory(target.Path ?? "/")
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
        using var client = CreateClient(target);
        client.Connect();
        client.DeleteFile(CombinePath(target.Path, remoteName));
        return Task.CompletedTask;
    }

    public Task<(bool Success, string? Error)> TestConnectionAsync(BackupTarget target, CancellationToken ct)
    {
        try
        {
            using var client = CreateClient(target);
            client.Connect();
            client.ListDirectory(target.Path ?? "/");
            return Task.FromResult<(bool, string?)>((true, null));
        }
        catch (Exception ex)
        {
            return Task.FromResult<(bool, string?)>((false, ExceptionHelper.Describe(ex)));
        }
    }

    private static SftpClient CreateClient(BackupTarget target)
    {
        if (string.IsNullOrWhiteSpace(target.Host) || string.IsNullOrWhiteSpace(target.Username))
            throw new InvalidOperationException($"SFTP-Ziel '{target.Name}' ist unvollständig konfiguriert (Host/Benutzername fehlt)");

        if (!string.IsNullOrEmpty(target.PrivateKey))
        {
            var keyFile = new PrivateKeyFile(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(target.PrivateKey)));
            return new SftpClient(target.Host, target.Port ?? 22, target.Username, keyFile);
        }

        return new SftpClient(target.Host, target.Port ?? 22, target.Username, target.Password ?? string.Empty);
    }

    private static string CombinePath(string? basePath, string fileName) =>
        (string.IsNullOrEmpty(basePath) ? "/" + fileName : basePath.TrimEnd('/') + "/" + fileName);
}
