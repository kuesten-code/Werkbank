using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Kuestencode.Werkbank.Host.Models;
using Kuestencode.Werkbank.Host.Services;

namespace Kuestencode.Werkbank.Host.Services.Backup.Providers;

public class S3BackupProvider : IBackupTargetProvider
{
    public async Task UploadAsync(BackupTarget target, string localPath, string remoteName, CancellationToken ct)
    {
        using var client = CreateClient(target);
        var (bucket, prefix) = ParsePath(target.Path!);
        var key = BuildKey(prefix, remoteName);

        using var fileStream = File.OpenRead(localPath);
        await client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            InputStream = fileStream
        }, ct);
    }

    public async Task DownloadAsync(BackupTarget target, string remoteName, string localPath, CancellationToken ct = default)
    {
        using var client = CreateClient(target);
        var (bucket, prefix) = ParsePath(target.Path!);
        var key = BuildKey(prefix, remoteName);

        using var response = await client.GetObjectAsync(bucket, key, ct);
        using var fileStream = File.Create(localPath);
        await response.ResponseStream.CopyToAsync(fileStream, ct);
    }

    public async Task<List<BackupFileInfo>> ListFilesAsync(BackupTarget target, CancellationToken ct)
    {
        using var client = CreateClient(target);
        var (bucket, prefix) = ParsePath(target.Path!);

        var response = await client.ListObjectsV2Async(new ListObjectsV2Request
        {
            BucketName = bucket,
            Prefix = prefix
        }, ct);

        return response.S3Objects
            .Select(o => Path.GetFileName(o.Key))
            .Where(BackupFileNaming.IsBackupFile)
            .Select(name =>
            {
                var date = BackupFileNaming.ParseDateFromFileName(name);
                var size = response.S3Objects.First(o => Path.GetFileName(o.Key) == name).Size;
                return new BackupFileInfo(name, date, size, BackupFileNaming.DetermineBackupType(date));
            })
            .ToList();
    }

    public async Task DeleteAsync(BackupTarget target, string remoteName, CancellationToken ct)
    {
        using var client = CreateClient(target);
        var (bucket, prefix) = ParsePath(target.Path!);
        await client.DeleteObjectAsync(bucket, BuildKey(prefix, remoteName), ct);
    }

    public async Task<(bool Success, string? Error)> TestConnectionAsync(BackupTarget target, CancellationToken ct)
    {
        try
        {
            using var client = CreateClient(target);
            var (bucket, _) = ParsePath(target.Path!);
            await client.ListObjectsV2Async(new ListObjectsV2Request { BucketName = bucket, MaxKeys = 1 }, ct);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ExceptionHelper.Describe(ex));
        }
    }

    private static AmazonS3Client CreateClient(BackupTarget target)
    {
        var config = new AmazonS3Config();

        // Custom Endpoint für Hetzner, MinIO etc.
        if (!string.IsNullOrEmpty(target.Host))
        {
            config.ServiceURL = target.Host.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? target.Host : $"https://{target.Host}";
            config.ForcePathStyle = true;
        }

        if (!string.IsNullOrEmpty(target.Region))
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(target.Region);

        return new AmazonS3Client(target.AccessKey, target.SecretKey, config);
    }

    private static (string bucket, string prefix) ParsePath(string path)
    {
        var parts = path.TrimStart('/').Split('/', 2);
        return (parts[0], parts.Length > 1 ? parts[1] : "");
    }

    private static string BuildKey(string prefix, string remoteName) =>
        string.IsNullOrEmpty(prefix) ? remoteName : $"{prefix}/{remoteName}";
}
