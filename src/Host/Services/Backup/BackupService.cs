using System.Diagnostics;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Kuestencode.Core.Interfaces;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Models;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Host.Services.Backup;

/// <summary>
/// Sichert den kompletten data/-Ordner (PostgreSQL-Daten, Uploads, Keys) als tar.gz zu einem
/// oder mehreren konfigurierten Zielen. Läuft entweder manuell (Button) oder über
/// <see cref="BackupSchedulerService"/> (Cron).
/// </summary>
public class BackupService : IBackupService
{
    private readonly HostDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _configuration;
    private readonly IBackupTargetProviderFactory _providerFactory;
    private readonly PasswordEncryptionService _passwordEncryption;
    private readonly IEmailEngine _emailEngine;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BackupService> _logger;

    // Statisch, weil BackupService scoped registriert ist: ein laufendes Backup (manuell oder
    // per Scheduler) muss prozessweit erkannt werden, nicht nur innerhalb eines Scopes.
    private static int _isRunning;

    public BackupService(
        HostDbContext context,
        IWebHostEnvironment env,
        IConfiguration configuration,
        IBackupTargetProviderFactory providerFactory,
        PasswordEncryptionService passwordEncryption,
        IEmailEngine emailEngine,
        IHttpClientFactory httpClientFactory,
        ILogger<BackupService> logger)
    {
        _context = context;
        _env = env;
        _configuration = configuration;
        _providerFactory = providerFactory;
        _passwordEncryption = passwordEncryption;
        _emailEngine = emailEngine;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    // "Backup:SourcePath" zeigt in Docker auf den kompletten gemounteten ./data-Baum
    // (Postgres, Modul-Uploads, Keys - siehe docker-compose.yml). Ohne diese Konfiguration
    // (z.B. lokaler Start ohne Docker) fällt der Service auf den eigenen data/-Unterordner
    // zurück, wie er über ContentRootPath erreichbar ist.
    private string GetDataPath() =>
        _configuration["Backup:SourcePath"] ?? Path.Combine(_env.ContentRootPath, "data");

    public async Task<BackupResult> RunBackupAsync(bool isManual = false, CancellationToken ct = default)
    {
        if (Interlocked.CompareExchange(ref _isRunning, 1, 0) != 0)
            return new BackupResult(false, "Backup läuft bereits", new List<TargetResult>());

        string? tempArchivePath = null;

        try
        {
            var settings = await GetSettingsAsync();
            var enabledTargets = (await GetTargetsAsync()).Where(t => t.Enabled).ToList();

            if (enabledTargets.Count == 0)
                return new BackupResult(false, "Keine aktiven Backup-Ziele konfiguriert", new List<TargetResult>());

            var timestampUtc = DateTime.UtcNow;
            var (archivePath, fileName, backupType) = await CreateBackupArchiveAsync(settings, timestampUtc, ct);
            tempArchivePath = archivePath;
            var fileSize = new FileInfo(archivePath).Length;

            var results = new List<TargetResult>();
            foreach (var target in enabledTargets)
            {
                results.Add(await UploadToTargetAsync(target, archivePath, fileName, fileSize, backupType, isManual, ct));
            }

            foreach (var target in enabledTargets)
            {
                await ApplyRotationAsync(target, settings, ct);
            }

            var anyFailed = results.Any(r => !r.Success);
            if (anyFailed && settings.AlertOnFailure)
            {
                await SendFailureAlertAsync(settings, results);
            }

            return new BackupResult(!anyFailed, anyFailed ? "Mindestens ein Backup-Ziel ist fehlgeschlagen" : null, results);
        }
        finally
        {
            if (tempArchivePath != null && File.Exists(tempArchivePath))
                File.Delete(tempArchivePath);

            Interlocked.Exchange(ref _isRunning, 0);
        }
    }

    // AsNoTracking()/Detach überall in diesem Service, wo eine Entität an den Aufrufer
    // zurückgegeben wird: HostDbContext ist in Blazor Server pro Circuit scoped (nicht pro
    // Request), lebt also über Get-/Update-Aufrufe hinweg. Ohne Entkopplung würde EF Core
    // beim Update dieselbe bereits getrackte Instanz zurückliefern wie die vom Aufrufer
    // übergebene ("existing" == "settings") - der Vergleich "hat sich das Secret geändert"
    // wäre dann immer falsch, weil beide Seiten auf demselben Feld läsen.
    public async Task<BackupSettings> GetSettingsAsync()
    {
        var settings = await _context.BackupSettings.AsNoTracking().Include(s => s.Targets).FirstOrDefaultAsync();

        if (settings == null)
        {
            settings = new BackupSettings();
            _context.BackupSettings.Add(settings);
            await _context.SaveChangesAsync();
            _context.Entry(settings).State = EntityState.Detached;
        }

        return settings;
    }

    public async Task UpdateSettingsAsync(BackupSettings settings)
    {
        var existing = await _context.BackupSettings.FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Backup-Einstellungen nicht initialisiert");

        existing.Schedule = settings.Schedule;
        existing.Enabled = settings.Enabled;
        existing.EncryptionEnabled = settings.EncryptionEnabled;

        if (!string.IsNullOrWhiteSpace(settings.EncryptionPassword) && settings.EncryptionPassword != existing.EncryptionPassword)
            existing.EncryptionPassword = _passwordEncryption.Encrypt(settings.EncryptionPassword);

        existing.KeepDaily = settings.KeepDaily;
        existing.KeepWeekly = settings.KeepWeekly;
        existing.KeepMonthly = settings.KeepMonthly;
        existing.KeepYearly = settings.KeepYearly;
        existing.AlertOnFailure = settings.AlertOnFailure;
        existing.AlertEmail = settings.AlertEmail;
        existing.AlertWebhookUrl = settings.AlertWebhookUrl;
        existing.WarnAfterDays = settings.WarnAfterDays;

        await _context.SaveChangesAsync();
    }

    public Task<List<BackupTarget>> GetTargetsAsync() =>
        _context.BackupTargets.AsNoTracking().OrderBy(t => t.Name).ToListAsync();

    public async Task<BackupTarget> AddTargetAsync(BackupTarget target)
    {
        var settings = await GetSettingsAsync();
        target.BackupSettingsId = settings.Id;

        if (!string.IsNullOrWhiteSpace(target.Password))
            target.Password = _passwordEncryption.Encrypt(target.Password);
        if (!string.IsNullOrWhiteSpace(target.PrivateKey))
            target.PrivateKey = _passwordEncryption.Encrypt(target.PrivateKey);
        if (!string.IsNullOrWhiteSpace(target.SecretKey))
            target.SecretKey = _passwordEncryption.Encrypt(target.SecretKey);

        _context.BackupTargets.Add(target);
        await _context.SaveChangesAsync();
        _context.Entry(target).State = EntityState.Detached;
        return target;
    }

    public async Task UpdateTargetAsync(BackupTarget target)
    {
        var existing = await _context.BackupTargets.FindAsync(target.Id)
            ?? throw new InvalidOperationException($"Backup-Ziel {target.Id} nicht gefunden");

        existing.Name = target.Name;
        existing.Type = target.Type;
        existing.Enabled = target.Enabled;
        existing.Path = target.Path;
        existing.Host = target.Host;
        existing.Port = target.Port;
        existing.UseHttps = target.UseHttps;
        existing.Username = target.Username;
        existing.AccessKey = target.AccessKey;
        existing.Region = target.Region;

        if (!string.IsNullOrWhiteSpace(target.Password) && target.Password != existing.Password)
            existing.Password = _passwordEncryption.Encrypt(target.Password);
        if (!string.IsNullOrWhiteSpace(target.PrivateKey) && target.PrivateKey != existing.PrivateKey)
            existing.PrivateKey = _passwordEncryption.Encrypt(target.PrivateKey);
        if (!string.IsNullOrWhiteSpace(target.SecretKey) && target.SecretKey != existing.SecretKey)
            existing.SecretKey = _passwordEncryption.Encrypt(target.SecretKey);

        await _context.SaveChangesAsync();
    }

    public async Task DeleteTargetAsync(int targetId)
    {
        var target = await _context.BackupTargets.FindAsync(targetId);
        if (target == null)
            return;

        _context.BackupTargets.Remove(target);
        await _context.SaveChangesAsync();
    }

    public async Task<(bool Success, string? Error)> TestTargetConnectionAsync(int targetId)
    {
        var target = await _context.BackupTargets.FindAsync(targetId)
            ?? throw new InvalidOperationException($"Backup-Ziel {targetId} nicht gefunden");

        var provider = _providerFactory.GetProvider(target.Type);
        return await provider.TestConnectionAsync(DecryptForConnection(target), CancellationToken.None);
    }

    public Task<List<BackupHistory>> GetHistoryAsync(int? targetId = null, int limit = 50)
    {
        var query = _context.BackupHistory.AsNoTracking().Include(h => h.Target).AsQueryable();

        if (targetId.HasValue)
            query = query.Where(h => h.BackupTargetId == targetId.Value);

        return query.OrderByDescending(h => h.StartedAt).Take(limit).ToListAsync();
    }

    public async Task<List<BackupFileInfo>> ListBackupsOnTargetAsync(int targetId)
    {
        var target = await _context.BackupTargets.FindAsync(targetId)
            ?? throw new InvalidOperationException($"Backup-Ziel {targetId} nicht gefunden");

        var provider = _providerFactory.GetProvider(target.Type);
        return await provider.ListFilesAsync(DecryptForConnection(target), CancellationToken.None);
    }

    public async Task<Stream> OpenBackupFileForDownloadAsync(int targetId, string fileName)
    {
        var target = await _context.BackupTargets.FindAsync(targetId)
            ?? throw new InvalidOperationException($"Backup-Ziel {targetId} nicht gefunden");

        var provider = _providerFactory.GetProvider(target.Type);
        var tempPath = Path.Combine(Path.GetTempPath(), $"download-{Guid.NewGuid()}-{fileName}");
        await provider.DownloadAsync(DecryptForConnection(target), fileName, tempPath);

        return new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.DeleteOnClose);
    }

    public async Task<RestoreResult> RestoreAsync(int targetId, string fileName)
    {
        var settings = await GetSettingsAsync();
        var target = await _context.BackupTargets.FindAsync(targetId);

        if (target == null)
            return new RestoreResult(false, "Backup-Ziel nicht gefunden");

        // dataPath ist im Container i.d.R. selbst ein Docker-Bind-Mount (./data). Ein
        // Mountpoint lässt sich nicht per rename() verschieben - genau das machte
        // "Directory.Move(dataPath, preRestorePath)" vorher, und der Kernel quittiert das mit
        // EBUSY ("Device or resource busy"). Die Sicherung der aktuellen Daten liegt deshalb
        // INNERHALB von dataPath (selbes Dateisystem, ganz normale Unterordner/Dateien lassen
        // sich dort problemlos verschieben).
        var dataPath = GetDataPath();
        var preRestoreDirName = ".pre-restore-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var preRestorePath = Path.Combine(dataPath, preRestoreDirName);
        var tempPath = Path.Combine(Path.GetTempPath(), fileName);

        try
        {
            var provider = _providerFactory.GetProvider(target.Type);
            var connectionTarget = DecryptForConnection(target);

            await provider.DownloadAsync(connectionTarget, fileName, tempPath);

            var archivePath = tempPath;
            if (fileName.EndsWith(".enc", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(settings.EncryptionPassword))
                {
                    File.Delete(tempPath);
                    return new RestoreResult(false, "Verschlüsseltes Backup, aber kein Passwort konfiguriert");
                }

                archivePath = tempPath[..^".enc".Length];
                await DecryptFileAsync(tempPath, archivePath, _passwordEncryption.Decrypt(settings.EncryptionPassword));
                File.Delete(tempPath);
            }

            // Reste eines vorherigen, nicht sauber abgeschlossenen Restores zuerst ZURÜCKSPIELEN,
            // niemals blind löschen: ein .pre-restore-* Ordner kann der einzige verbliebene Ort
            // für Daten sein, die im vorherigen Versuch schon aus dataPath herausbewegt wurden
            // (genau das hat vorher den "keys"-Ordner gekostet, als ein Verschieben mittendrin
            // an einer Berechtigung scheiterte).
            foreach (var stale in Directory.GetDirectories(dataPath, ".pre-restore-*"))
            {
                MoveDirectoryContents(stale, dataPath, excludeName: null);
                if (!Directory.EnumerateFileSystemEntries(stale).Any())
                    Directory.Delete(stale, true);
                // Falls trotzdem noch etwas übrig ist (Namenskollision), bewusst liegen lassen -
                // lieber eine Datenleiche als stillschweigend etwas zu überschreiben/verlieren.
            }

            Directory.CreateDirectory(preRestorePath);
            try
            {
                MoveDirectoryContents(dataPath, preRestorePath, excludeName: preRestoreDirName);
            }
            catch
            {
                // Auch dieser Schritt kann mittendrin scheitern (z.B. fehlende Berechtigung auf
                // einem bestimmten Unterordner) - alles bereits Verschobene sofort zurückholen,
                // bevor der Fehler nach außen gereicht wird.
                MoveDirectoryContents(preRestorePath, dataPath, excludeName: null);
                if (!Directory.EnumerateFileSystemEntries(preRestorePath).Any())
                    Directory.Delete(preRestorePath, true);
                throw;
            }

            try
            {
                // Archiv enthält "data/..." als Top-Level-Eintrag (siehe CreateTarGzAsync) -> in
                // den *Elternordner* von dataPath entpacken, nicht in dataPath selbst.
                await ExtractTarGzAsync(archivePath, Path.GetDirectoryName(dataPath) ?? ".", CancellationToken.None);
            }
            catch
            {
                // Rollback: alles gerade Extrahierte verwerfen, alte Daten zurückholen.
                foreach (var entry in Directory.GetFileSystemEntries(dataPath))
                {
                    if (Path.GetFileName(entry) == preRestoreDirName) continue;
                    if (Directory.Exists(entry)) Directory.Delete(entry, true);
                    else File.Delete(entry);
                }
                MoveDirectoryContents(preRestorePath, dataPath, excludeName: null);
                Directory.Delete(preRestorePath, true);
                throw;
            }

            File.Delete(archivePath);
            Directory.Delete(preRestorePath, true);

            _logger.LogWarning("Restore abgeschlossen. Neustart erforderlich!");

            return new RestoreResult(true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Restore fehlgeschlagen");
            return new RestoreResult(false, ex.Message);
        }
    }

    // Kopieren+Löschen statt Directory.Move/File.Move (= rename()): auf manchen Docker-Desktop/
    // WSL2-Bind-Mount-Übersetzungen scheitert rename() an Verzeichnissen mit restriktiven Rechten
    // (z.B. Postgres' eigener 0700-Datenordner, Owner uid 999) - selbst wenn der Host-Container
    // als root läuft und lesend/schreibend durchaus zugreifen kann ("Access denied" beim reinen
    // Verschieben). Kopieren+anschließendes Löschen nutzt andere Syscalls und kommt damit öfter
    // durch; im schlimmsten Fall bleibt bei einem fehlschlagenden Lösch-Schritt eine doppelte
    // Kopie liegen (Platzverschwendung), aber nichts geht verloren.
    private static void MoveDirectoryContents(string sourceDir, string targetDir, string? excludeName)
    {
        foreach (var entry in Directory.GetFileSystemEntries(sourceDir))
        {
            var name = Path.GetFileName(entry);
            if (name == excludeName) continue;

            var destination = Path.Combine(targetDir, name);

            // Ziel existiert schon (z.B. Rest eines anderen, ungewöhnlich abgebrochenen
            // Versuchs) - nicht überschreiben/löschen, sondern liegen lassen. Lieber eine
            // Datenleiche im Quellordner als versehentlich etwas zu vernichten.
            if (Directory.Exists(destination) || File.Exists(destination))
                continue;

            if (Directory.Exists(entry))
            {
                CopyDirectoryRecursive(entry, destination);
                Directory.Delete(entry, true);
            }
            else
            {
                File.Copy(entry, destination);
                File.Delete(entry);
            }
        }
    }

    private static void CopyDirectoryRecursive(string sourceDir, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);

        foreach (var file in Directory.GetFiles(sourceDir))
            File.Copy(file, Path.Combine(destinationDir, Path.GetFileName(file)));

        foreach (var subDir in Directory.GetDirectories(sourceDir))
            CopyDirectoryRecursive(subDir, Path.Combine(destinationDir, Path.GetFileName(subDir)));
    }

    public async Task<BackupStatusInfo> GetStatusAsync()
    {
        var settings = await GetSettingsAsync();
        var isRunning = Volatile.Read(ref _isRunning) == 1;

        var lastSuccess = await _context.BackupHistory
            .Where(h => h.Status == BackupStatus.Success)
            .OrderByDescending(h => h.CompletedAt)
            .Select(h => h.CompletedAt)
            .FirstOrDefaultAsync();

        if (lastSuccess == null)
            return new BackupStatusInfo(null, settings.Enabled, 0, isRunning);

        var days = (int)(DateTime.UtcNow - lastSuccess.Value).TotalDays;
        var isOverdue = settings.Enabled && days > settings.WarnAfterDays;

        return new BackupStatusInfo(lastSuccess, isOverdue, days, isRunning);
    }

    private async Task<TargetResult> UploadToTargetAsync(
        BackupTarget target, string archivePath, string fileName, long fileSize, BackupType backupType, bool isManual, CancellationToken ct)
    {
        var history = new BackupHistory
        {
            BackupTargetId = target.Id,
            StartedAt = DateTime.UtcNow,
            Status = BackupStatus.Running,
            FileName = fileName,
            Type = backupType,
            IsManual = isManual
        };
        _context.BackupHistory.Add(history);
        await _context.SaveChangesAsync(ct);

        // target kommt aus GetTargetsAsync() (AsNoTracking) - für den Statusschreibvorgang
        // getrennt getrackt nachladen statt der übergebenen Instanz.
        var trackedTarget = await _context.BackupTargets.FindAsync(new object?[] { target.Id }, ct) ?? target;

        try
        {
            var provider = _providerFactory.GetProvider(target.Type);
            await provider.UploadAsync(DecryptForConnection(target), archivePath, fileName, ct);

            history.Status = BackupStatus.Success;
            history.CompletedAt = DateTime.UtcNow;
            history.FileSizeBytes = fileSize;

            trackedTarget.LastBackupAt = DateTime.UtcNow;
            trackedTarget.LastBackupSuccess = true;
            trackedTarget.LastBackupError = null;

            await _context.SaveChangesAsync(ct);

            return new TargetResult(target.Id, target.Name, true, null, fileSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup zu {Target} fehlgeschlagen", target.Name);
            var errorMessage = ExceptionHelper.Describe(ex);

            history.Status = BackupStatus.Failed;
            history.CompletedAt = DateTime.UtcNow;
            history.ErrorMessage = errorMessage;

            trackedTarget.LastBackupAt = DateTime.UtcNow;
            trackedTarget.LastBackupSuccess = false;
            trackedTarget.LastBackupError = errorMessage;

            await _context.SaveChangesAsync(ct);

            return new TargetResult(target.Id, target.Name, false, errorMessage, null);
        }
    }

    private async Task ApplyRotationAsync(BackupTarget target, BackupSettings settings, CancellationToken ct)
    {
        var provider = _providerFactory.GetProvider(target.Type);
        List<BackupFileInfo> files;

        try
        {
            files = await provider.ListFilesAsync(DecryptForConnection(target), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rotation für {Target} übersprungen: Dateiliste konnte nicht geladen werden", target.Name);
            return;
        }

        var toDelete = SelectExcess(files, BackupType.Daily, settings.KeepDaily)
            .Concat(SelectExcess(files, BackupType.Weekly, settings.KeepWeekly))
            .Concat(SelectExcess(files, BackupType.Monthly, settings.KeepMonthly))
            .Concat(SelectExcess(files, BackupType.Yearly, settings.KeepYearly));

        foreach (var fileName in toDelete)
        {
            try
            {
                await provider.DeleteAsync(DecryptForConnection(target), fileName, ct);
                _logger.LogInformation("Backup {FileName} auf {Target} gelöscht (Rotation)", fileName, target.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Löschen von {FileName} auf {Target} fehlgeschlagen (Rotation)", fileName, target.Name);
            }
        }
    }

    private static IEnumerable<string> SelectExcess(List<BackupFileInfo> files, BackupType type, int keep) =>
        files.Where(f => f.Type == type)
            .OrderByDescending(f => f.Date)
            .Skip(keep)
            .Select(f => f.FileName);

    private async Task SendFailureAlertAsync(BackupSettings settings, List<TargetResult> results)
    {
        var failed = results.Where(r => !r.Success).ToList();

        if (!string.IsNullOrWhiteSpace(settings.AlertEmail))
        {
            try
            {
                var body = "<p>Folgende Backup-Ziele sind fehlgeschlagen:</p><ul>" +
                    string.Join("", failed.Select(f => $"<li>{f.TargetName}: {f.Error}</li>")) + "</ul>";
                await _emailEngine.SendEmailAsync(settings.AlertEmail, "Backup fehlgeschlagen", body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Backup-Alert-E-Mail konnte nicht gesendet werden");
            }
        }

        if (!string.IsNullOrWhiteSpace(settings.AlertWebhookUrl))
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var payload = new { text = $"Backup fehlgeschlagen: {string.Join(", ", failed.Select(f => f.TargetName))}" };
                await client.PostAsJsonAsync(settings.AlertWebhookUrl, payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Backup-Alert-Webhook konnte nicht ausgelöst werden");
            }
        }
    }

    private async Task<(string ArchivePath, string FileName, BackupType Type)> CreateBackupArchiveAsync(
        BackupSettings settings, DateTime timestampUtc, CancellationToken ct)
    {
        var dataPath = GetDataPath();
        var fileName = BackupFileNaming.BuildFileName(timestampUtc);
        var tempPath = Path.Combine(Path.GetTempPath(), fileName);

        await CreateTarGzAsync(dataPath, tempPath, ct);

        if (settings.EncryptionEnabled && !string.IsNullOrWhiteSpace(settings.EncryptionPassword))
        {
            var encryptedPath = tempPath + ".enc";
            await EncryptFileAsync(tempPath, encryptedPath, _passwordEncryption.Decrypt(settings.EncryptionPassword));
            File.Delete(tempPath);
            tempPath = encryptedPath;
            fileName += ".enc";
        }

        return (tempPath, fileName, BackupFileNaming.DetermineBackupType(timestampUtc));
    }

    private static async Task CreateTarGzAsync(string sourceDir, string outputPath, CancellationToken ct)
    {
        var parentDir = Path.GetDirectoryName(sourceDir) ?? ".";
        var dirName = Path.GetFileName(sourceDir);

        var processInfo = new ProcessStartInfo("tar") { RedirectStandardError = true, UseShellExecute = false };
        processInfo.ArgumentList.Add("czf");
        processInfo.ArgumentList.Add(outputPath);
        processInfo.ArgumentList.Add("-C");
        processInfo.ArgumentList.Add(parentDir);
        processInfo.ArgumentList.Add(dirName);

        using var process = Process.Start(processInfo)!;
        var error = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"tar fehlgeschlagen: {error}");
    }

    private static async Task ExtractTarGzAsync(string archivePath, string targetDir, CancellationToken ct)
    {
        var processInfo = new ProcessStartInfo("tar") { RedirectStandardError = true, UseShellExecute = false };
        processInfo.ArgumentList.Add("xzf");
        processInfo.ArgumentList.Add(archivePath);
        processInfo.ArgumentList.Add("-C");
        processInfo.ArgumentList.Add(targetDir);

        using var process = Process.Start(processInfo)!;
        var error = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"tar fehlgeschlagen: {error}");
    }

    private static async Task EncryptFileAsync(string inputPath, string outputPath, string password)
    {
        using var inputStream = File.OpenRead(inputPath);
        using var outputStream = File.Create(outputPath);

        using var aes = Aes.Create();
        aes.KeySize = 256;

        var salt = RandomNumberGenerator.GetBytes(16);
        using var keyDerivation = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        aes.Key = keyDerivation.GetBytes(32);
        aes.IV = RandomNumberGenerator.GetBytes(16);

        await outputStream.WriteAsync(salt);
        await outputStream.WriteAsync(aes.IV);

        await using var cryptoStream = new CryptoStream(outputStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
        await inputStream.CopyToAsync(cryptoStream);
        await cryptoStream.FlushFinalBlockAsync();
    }

    private static async Task DecryptFileAsync(string inputPath, string outputPath, string password)
    {
        using var inputStream = File.OpenRead(inputPath);

        var salt = new byte[16];
        var iv = new byte[16];
        await inputStream.ReadExactlyAsync(salt);
        await inputStream.ReadExactlyAsync(iv);

        using var aes = Aes.Create();
        aes.KeySize = 256;
        using var keyDerivation = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        aes.Key = keyDerivation.GetBytes(32);
        aes.IV = iv;

        using var outputStream = File.Create(outputPath);
        await using var cryptoStream = new CryptoStream(inputStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
        await cryptoStream.CopyToAsync(outputStream);
    }

    private BackupTarget DecryptForConnection(BackupTarget target) => new()
    {
        Id = target.Id,
        BackupSettingsId = target.BackupSettingsId,
        Name = target.Name,
        Type = target.Type,
        Enabled = target.Enabled,
        Path = target.Path,
        Host = target.Host,
        Port = target.Port,
        UseHttps = target.UseHttps,
        Username = target.Username,
        Password = DecryptOrEmpty(target.Password),
        PrivateKey = DecryptOrEmpty(target.PrivateKey),
        AccessKey = target.AccessKey,
        SecretKey = DecryptOrEmpty(target.SecretKey),
        Region = target.Region
    };

    private string? DecryptOrEmpty(string? cipherText) =>
        string.IsNullOrEmpty(cipherText) ? cipherText : _passwordEncryption.Decrypt(cipherText);
}
