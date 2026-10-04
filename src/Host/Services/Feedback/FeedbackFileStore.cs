namespace Kuestencode.Werkbank.Host.Services.Feedback;

/// <summary>
/// Ablage von Screenshots unter data/feedback (wird vom Backup mitgesichert). Pfade bestehen
/// ausschließlich aus serverseitig erzeugten Bestandteilen und werden beim Zugriff zusätzlich
/// gegen das Basisverzeichnis geprüft.
/// </summary>
public class FeedbackFileStore : IFeedbackFileStore
{
    private readonly string _basePath;

    public FeedbackFileStore(IConfiguration configuration, IWebHostEnvironment env)
    {
        _basePath = Path.GetFullPath(configuration["Feedback:StoragePath"]
            ?? Path.Combine(env.ContentRootPath, "data", "feedback"));
    }

    public async Task<string> SaveAsync(string folder, string extension, byte[] content, CancellationToken ct = default)
    {
        var relativePath = $"{folder}/{Guid.NewGuid():N}{extension}";
        var fullPath = ResolveFullPath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllBytesAsync(fullPath, content, ct);
        return relativePath;
    }

    public Stream OpenRead(string relativePath) => File.OpenRead(ResolveFullPath(relativePath));

    public Task<byte[]> ReadAllBytesAsync(string relativePath, CancellationToken ct = default) =>
        File.ReadAllBytesAsync(ResolveFullPath(relativePath), ct);

    public void Delete(string relativePath)
    {
        var fullPath = ResolveFullPath(relativePath);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }

    private string ResolveFullPath(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_basePath, relativePath));
        if (!fullPath.StartsWith(_basePath + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidOperationException("Ungültiger Dateipfad.");
        return fullPath;
    }
}
