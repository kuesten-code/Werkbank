using System.Net.Http.Headers;
using System.Text;
using Kuestencode.Werkbank.Host.Models;
using Kuestencode.Werkbank.Host.Services;
using WebDav;

namespace Kuestencode.Werkbank.Host.Services.Backup.Providers;

// WebDav.Client bietet auf den hier genutzten Überladungen kein CancellationToken an
// (siehe https://github.com/skazantsev/WebDavClient) - das Interface hält es trotzdem für
// die anderen Provider, wird hier aber schlicht nicht durchgereicht.
//
// WICHTIG: WebDav.Client wirft bei HTTP-Fehlern (4xx/5xx) i.d.R. KEINE Exception, sondern
// liefert ein WebDavResponse mit IsSuccessful=false zurück - deshalb wird das hier für jede
// Operation explizit geprüft. Ohne diese Prüfung würde ein serverseitig fehlgeschlagener
// Upload (z.B. "permission denied" beim rclone-Testserver) fälschlich als erfolgreiches
// Backup durchgehen.
//
// Basic-Auth-Header wird PRÄVENTIV mitgeschickt (nicht über WebDavClientParams.Credentials,
// das löst erst auf 401-Challenge hin einen Retry aus): bei einem großen gestreamten Body
// (unser tar.gz) kappt der Server die Verbindung oft schon während der unauthentifizierten
// Erstanfrage, was als "unable to write data to transport connection: broken pipe" auftaucht.
public class WebDavBackupProvider : IBackupTargetProvider
{
    public async Task UploadAsync(BackupTarget target, string localPath, string remoteName, CancellationToken ct)
    {
        using var httpClient = CreateHttpClient(target);
        using var client = new WebDavClient(httpClient);
        using var fileStream = File.OpenRead(localPath);
        var response = await client.PutFile(CombinePath(target.Path, remoteName), fileStream);
        EnsureSuccessful(response, "Hochladen");
    }

    public async Task DownloadAsync(BackupTarget target, string remoteName, string localPath, CancellationToken ct = default)
    {
        using var httpClient = CreateHttpClient(target);
        using var client = new WebDavClient(httpClient);
        using var response = await client.GetRawFile(CombinePath(target.Path, remoteName));
        EnsureSuccessful(response, "Herunterladen");
        using var fileStream = File.Create(localPath);
        await response.Stream.CopyToAsync(fileStream, ct);
    }

    public async Task<List<BackupFileInfo>> ListFilesAsync(BackupTarget target, CancellationToken ct)
    {
        using var httpClient = CreateHttpClient(target);
        using var client = new WebDavClient(httpClient);
        var response = await client.Propfind(target.Path ?? "/");
        EnsureSuccessful(response, "Auflisten");

        return response.Resources
            .Where(r => !string.IsNullOrEmpty(r.DisplayName) && BackupFileNaming.IsBackupFile(r.DisplayName))
            .Select(r =>
            {
                var date = BackupFileNaming.ParseDateFromFileName(r.DisplayName!);
                return new BackupFileInfo(r.DisplayName!, date, r.ContentLength ?? 0, BackupFileNaming.DetermineBackupType(date));
            })
            .ToList();
    }

    public async Task DeleteAsync(BackupTarget target, string remoteName, CancellationToken ct)
    {
        using var httpClient = CreateHttpClient(target);
        using var client = new WebDavClient(httpClient);
        var response = await client.Delete(CombinePath(target.Path, remoteName));
        EnsureSuccessful(response, "Löschen");
    }

    public async Task<(bool Success, string? Error)> TestConnectionAsync(BackupTarget target, CancellationToken ct)
    {
        try
        {
            using var httpClient = CreateHttpClient(target);
            using var client = new WebDavClient(httpClient);
            var response = await client.Propfind(target.Path ?? "/");
            return response.IsSuccessful
                ? (true, null)
                : (false, $"HTTP {response.StatusCode}: {response.Description}");
        }
        catch (Exception ex)
        {
            return (false, ExceptionHelper.Describe(ex));
        }
    }

    private static void EnsureSuccessful(WebDavResponse response, string operation)
    {
        if (!response.IsSuccessful)
            throw new IOException($"WebDAV-{operation} fehlgeschlagen: HTTP {response.StatusCode} {response.Description}");
    }

    private static HttpClient CreateHttpClient(BackupTarget target)
    {
        var scheme = target.UseHttps ? "https" : "http";
        var port = target.Port ?? (target.UseHttps ? 443 : 80);

        var httpClient = new HttpClient
        {
            BaseAddress = new Uri($"{scheme}://{target.Host}:{port}")
        };

        if (!string.IsNullOrEmpty(target.Username))
        {
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{target.Username}:{target.Password}"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        }

        return httpClient;
    }

    private static string CombinePath(string? basePath, string fileName) =>
        string.IsNullOrEmpty(basePath) ? "/" + fileName : basePath.TrimEnd('/') + "/" + fileName;
}
