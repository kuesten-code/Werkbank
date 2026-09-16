using System.Diagnostics;
using FluentAssertions;
using Kuestencode.Core.Interfaces;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Models;
using Kuestencode.Werkbank.Host.Services;
using Kuestencode.Werkbank.Host.Services.Backup;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Kuestencode.Host.Tests.Services.Backup;

public class BackupServiceTests : IDisposable
{
    private readonly HostDbContext _context;
    private readonly Mock<IBackupTargetProviderFactory> _providerFactory = new();
    private readonly Mock<IBackupTargetProvider> _provider = new();
    private readonly Mock<IEmailEngine> _emailEngine = new();
    private readonly Mock<IWebHostEnvironment> _env = new();
    private readonly PasswordEncryptionService _passwordEncryption;
    private readonly string _contentRoot;
    private readonly BackupService _service;

    public BackupServiceTests()
    {
        var options = new DbContextOptionsBuilder<HostDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new HostDbContext(options);

        _contentRoot = Directory.CreateTempSubdirectory("werkbank-backup-test-").FullName;
        Directory.CreateDirectory(Path.Combine(_contentRoot, "data"));
        File.WriteAllText(Path.Combine(_contentRoot, "data", "dummy.txt"), "content");
        _env.Setup(e => e.ContentRootPath).Returns(_contentRoot);

        _providerFactory.Setup(f => f.GetProvider(It.IsAny<BackupTargetType>())).Returns(_provider.Object);
        _provider.Setup(p => p.ListFilesAsync(It.IsAny<BackupTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BackupFileInfo>());

        _passwordEncryption = new PasswordEncryptionService(new EphemeralDataProtectionProvider());
        var configuration = new ConfigurationBuilder().Build(); // "Backup:SourcePath" fehlt bewusst -> Fallback auf ContentRootPath/data

        _service = new BackupService(
            _context, _env.Object, configuration, _providerFactory.Object, _passwordEncryption,
            _emailEngine.Object, Mock.Of<IHttpClientFactory>(), new BackupScheduleChangeSignal(),
            NullLogger<BackupService>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_contentRoot))
            Directory.Delete(_contentRoot, true);
    }

    private async Task<BackupTarget> SeedTargetAsync(bool enabled = true, string password = "geheim")
    {
        return await _service.AddTargetAsync(new BackupTarget
        {
            Name = "Ziel",
            Type = BackupTargetType.Sftp,
            Enabled = enabled,
            Host = "example.com",
            Username = "user",
            Password = password
        });
    }

    // ─── Settings ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSettingsAsync_ErstelltStandardEinstellungenBeimErstenAufruf()
    {
        var settings = await _service.GetSettingsAsync();

        settings.Id.Should().BeGreaterThan(0);
        settings.Schedule.Should().Be("0 3 * * *");
        settings.Enabled.Should().BeFalse();
        settings.KeepDaily.Should().Be(7);
    }

    [Fact]
    public async Task UpdateSettingsAsync_VerschluesseltNeuesVerschluesselungspasswort()
    {
        var settings = await _service.GetSettingsAsync();
        settings.EncryptionEnabled = true;
        settings.EncryptionPassword = "mein-geheimnis";

        await _service.UpdateSettingsAsync(settings);

        var stored = await _context.BackupSettings.FirstAsync();
        stored.EncryptionPassword.Should().NotBe("mein-geheimnis");
        _passwordEncryption.Decrypt(stored.EncryptionPassword!).Should().Be("mein-geheimnis");
    }

    [Fact]
    public async Task UpdateSettingsAsync_LaesstUnveraendertesPasswortUnangetastet()
    {
        var settings = await _service.GetSettingsAsync();
        settings.EncryptionPassword = "mein-geheimnis";
        await _service.UpdateSettingsAsync(settings);
        var afterFirstSave = (await _context.BackupSettings.FirstAsync()).EncryptionPassword;

        var reloaded = await _service.GetSettingsAsync();
        reloaded.KeepDaily = 14; // andere Änderung, Passwort bleibt wie geladen (Ciphertext)
        await _service.UpdateSettingsAsync(reloaded);

        var afterSecondSave = await _context.BackupSettings.FirstAsync();
        afterSecondSave.EncryptionPassword.Should().Be(afterFirstSave);
        afterSecondSave.KeepDaily.Should().Be(14);
    }

    // ─── Targets ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddTargetAsync_VerschluesseltPasswortUndSetztBackupSettingsId()
    {
        var settings = await _service.GetSettingsAsync();

        var target = await SeedTargetAsync();

        target.BackupSettingsId.Should().Be(settings.Id);
        target.Password.Should().NotBe("geheim");
        _passwordEncryption.Decrypt(target.Password!).Should().Be("geheim");
    }

    [Fact]
    public async Task UpdateTargetAsync_OhneNeuesPasswort_BehaeltVerschluesseltesPasswort()
    {
        var target = await SeedTargetAsync();
        var storedCiphertext = target.Password;

        target.Name = "Umbenannt";
        target.Password = null; // UI schickt kein neues Passwort mit
        await _service.UpdateTargetAsync(target);

        var reloaded = await _context.BackupTargets.FindAsync(target.Id);
        reloaded!.Name.Should().Be("Umbenannt");
        reloaded.Password.Should().Be(storedCiphertext);
    }

    [Fact]
    public async Task UpdateTargetAsync_MitNeuemPasswort_VerschluesseltEsNeu()
    {
        var target = await SeedTargetAsync();

        target.Password = "neues-passwort";
        await _service.UpdateTargetAsync(target);

        var reloaded = await _context.BackupTargets.FindAsync(target.Id);
        _passwordEncryption.Decrypt(reloaded!.Password!).Should().Be("neues-passwort");
    }

    [Fact]
    public async Task DeleteTargetAsync_EntferntZiel()
    {
        var target = await SeedTargetAsync();

        await _service.DeleteTargetAsync(target.Id);

        (await _context.BackupTargets.FindAsync(target.Id)).Should().BeNull();
    }

    [Fact]
    public async Task TestTargetConnectionAsync_UebergibtProviderEntschluesselteZugangsdaten()
    {
        var target = await SeedTargetAsync();

        BackupTarget? received = null;
        _provider.Setup(p => p.TestConnectionAsync(It.IsAny<BackupTarget>(), It.IsAny<CancellationToken>()))
            .Callback<BackupTarget, CancellationToken>((t, _) => received = t)
            .ReturnsAsync((true, (string?)null));

        var (success, error) = await _service.TestTargetConnectionAsync(target.Id);

        success.Should().BeTrue();
        error.Should().BeNull();
        received.Should().NotBeNull();
        received!.Password.Should().Be("geheim");
    }

    [Fact]
    public async Task UpdateTargetAsync_SpeichertUseHttpsUndReichtEsAnDenProviderWeiter()
    {
        var target = await SeedTargetAsync();
        target.UseHttps = false;
        await _service.UpdateTargetAsync(target);

        BackupTarget? received = null;
        _provider.Setup(p => p.TestConnectionAsync(It.IsAny<BackupTarget>(), It.IsAny<CancellationToken>()))
            .Callback<BackupTarget, CancellationToken>((t, _) => received = t)
            .ReturnsAsync((true, (string?)null));

        await _service.TestTargetConnectionAsync(target.Id);

        received!.UseHttps.Should().BeFalse();
    }

    // ─── RunBackupAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task RunBackupAsync_OhneAktiveZiele_GibtFehlschlagZurueck()
    {
        await SeedTargetAsync(enabled: false);

        var result = await _service.RunBackupAsync();

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Keine aktiven Backup-Ziele konfiguriert");
        _provider.Verify(p => p.UploadAsync(It.IsAny<BackupTarget>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunBackupAsync_ErfolgreichesBackup_SchreibtHistoryUndAktualisiertZielStatus()
    {
        var target = await SeedTargetAsync();
        _provider.Setup(p => p.UploadAsync(It.IsAny<BackupTarget>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.RunBackupAsync(isManual: true);

        result.Success.Should().BeTrue();
        result.TargetResults.Should().ContainSingle(r => r.TargetId == target.Id && r.Success);

        var history = await _context.BackupHistory.SingleAsync();
        history.Status.Should().Be(BackupStatus.Success);
        history.IsManual.Should().BeTrue();
        history.FileSizeBytes.Should().BeGreaterThan(0);

        var reloadedTarget = await _context.BackupTargets.FindAsync(target.Id);
        reloadedTarget!.LastBackupSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RunBackupAsync_ProviderWirftAusnahme_SchreibtFehlgeschlageneHistoryUndSendetAlertMail()
    {
        var settings = await _service.GetSettingsAsync();
        settings.AlertEmail = "admin@example.com";
        await _service.UpdateSettingsAsync(settings);

        var target = await SeedTargetAsync();
        _provider.Setup(p => p.UploadAsync(It.IsAny<BackupTarget>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Verbindung fehlgeschlagen"));
        _emailEngine.Setup(e => e.SendEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                null, null, null, null, null, true))
            .ReturnsAsync(true);

        var result = await _service.RunBackupAsync();

        result.Success.Should().BeFalse();
        result.TargetResults.Should().ContainSingle(r => r.TargetId == target.Id && !r.Success);

        var history = await _context.BackupHistory.SingleAsync();
        history.Status.Should().Be(BackupStatus.Failed);
        history.ErrorMessage.Should().Be("Verbindung fehlgeschlagen");

        var reloadedTarget = await _context.BackupTargets.FindAsync(target.Id);
        reloadedTarget!.LastBackupSuccess.Should().BeFalse();
        reloadedTarget.LastBackupError.Should().Be("Verbindung fehlgeschlagen");

        _emailEngine.Verify(e => e.SendEmailAsync(
            "admin@example.com", "Backup fehlgeschlagen", It.IsAny<string>(),
            null, null, null, null, null, true), Times.Once);
    }

    [Fact]
    public async Task RunBackupAsync_WendetRotationAufBasisVonKeepDailyAn()
    {
        await SeedTargetAsync();
        _provider.Setup(p => p.UploadAsync(It.IsAny<BackupTarget>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // KeepDaily = 7 (Default) -> von 9 Tagesbackups müssen die 2 ältesten gelöscht werden.
        var files = Enumerable.Range(0, 9)
            .Select(i => new BackupFileInfo($"backup-daily-{i}.tar.gz", DateTime.UtcNow.AddDays(-i), 100, BackupType.Daily))
            .ToList();
        _provider.Setup(p => p.ListFilesAsync(It.IsAny<BackupTarget>(), It.IsAny<CancellationToken>())).ReturnsAsync(files);

        var deleted = new List<string>();
        _provider.Setup(p => p.DeleteAsync(It.IsAny<BackupTarget>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<BackupTarget, string, CancellationToken>((_, name, _) => deleted.Add(name))
            .Returns(Task.CompletedTask);

        await _service.RunBackupAsync();

        deleted.Should().BeEquivalentTo(new[] { "backup-daily-7.tar.gz", "backup-daily-8.tar.gz" });
    }

    [Fact]
    public async Task RunBackupAsync_WaehrendLaufendemBackup_LehntZweitenAufrufSofortAb()
    {
        await SeedTargetAsync();
        var uploadStarted = new TaskCompletionSource();
        var releaseUpload = new TaskCompletionSource();
        _provider.Setup(p => p.UploadAsync(It.IsAny<BackupTarget>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                uploadStarted.TrySetResult();
                await releaseUpload.Task;
            });

        var firstRun = _service.RunBackupAsync();
        await uploadStarted.Task;

        var secondResult = await _service.RunBackupAsync();

        secondResult.Success.Should().BeFalse();
        secondResult.Error.Should().Be("Backup läuft bereits");

        releaseUpload.SetResult();
        var firstResult = await firstRun;
        firstResult.Success.Should().BeTrue();
    }

    // ─── GetStatusAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetStatusAsync_OhneBackupsUndDeaktiviert_IstNichtUeberfaellig()
    {
        var status = await _service.GetStatusAsync();

        status.LastSuccessfulBackup.Should().BeNull();
        status.IsOverdue.Should().BeFalse();
        status.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task GetStatusAsync_NachErfolgreichemBackup_MeldetLetztenZeitpunkt()
    {
        await SeedTargetAsync();
        _provider.Setup(p => p.UploadAsync(It.IsAny<BackupTarget>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        await _service.RunBackupAsync();

        var status = await _service.GetStatusAsync();

        status.LastSuccessfulBackup.Should().NotBeNull();
        status.DaysSinceLastBackup.Should().Be(0);
    }

    [Fact]
    public async Task RunBackupAsync_MitAktivierterVerschluesselung_ErzeugtEncDateiDieSichWiederherstellenLaesst()
    {
        var settings = await _service.GetSettingsAsync();
        settings.EncryptionEnabled = true;
        settings.EncryptionPassword = "backup-geheimnis";
        await _service.UpdateSettingsAsync(settings);

        var target = await SeedTargetAsync();
        var keptArchivePath = Path.Combine(Path.GetTempPath(), "kept-" + Guid.NewGuid() + ".tar.gz.enc");

        string? uploadedFileName = null;
        _provider.Setup(p => p.UploadAsync(It.IsAny<BackupTarget>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<BackupTarget, string, string, CancellationToken>((_, localPath, remoteName, _) =>
            {
                uploadedFileName = remoteName;
                File.Copy(localPath, keptArchivePath, overwrite: true); // RunBackupAsync löscht localPath danach selbst
                return Task.CompletedTask;
            });

        try
        {
            var result = await _service.RunBackupAsync();
            result.Success.Should().BeTrue();
            uploadedFileName.Should().EndWith(".tar.gz.enc");

            _provider.Setup(p => p.DownloadAsync(It.IsAny<BackupTarget>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns<BackupTarget, string, string, CancellationToken>((_, _, localPath, _) =>
                {
                    File.Copy(keptArchivePath, localPath, overwrite: true);
                    return Task.CompletedTask;
                });

            var restoreResult = await _service.RestoreAsync(target.Id, uploadedFileName!);

            restoreResult.Success.Should().BeTrue();
            File.Exists(Path.Combine(_contentRoot, "data", "dummy.txt")).Should().BeTrue();
        }
        finally
        {
            if (File.Exists(keptArchivePath)) File.Delete(keptArchivePath);
        }
    }

    [Fact]
    public async Task RunBackupAsync_VerwendetKonfigurierbarenQuellPfadFallsGesetzt()
    {
        var customSourceDir = Directory.CreateTempSubdirectory("werkbank-backup-customsource-").FullName;
        string? keptArchivePath = null;

        try
        {
            await File.WriteAllTextAsync(Path.Combine(customSourceDir, "marker.txt"), "aus custom source");

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Backup:SourcePath"] = customSourceDir })
                .Build();
            var serviceWithCustomSource = new BackupService(
                _context, _env.Object, configuration, _providerFactory.Object, _passwordEncryption,
                _emailEngine.Object, Mock.Of<IHttpClientFactory>(), new BackupScheduleChangeSignal(),
                NullLogger<BackupService>.Instance);

            await SeedTargetAsync();
            _provider.Setup(p => p.UploadAsync(It.IsAny<BackupTarget>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns<BackupTarget, string, string, CancellationToken>((_, localPath, _, _) =>
                {
                    keptArchivePath = localPath + ".kept";
                    File.Copy(localPath, keptArchivePath); // RunBackupAsync löscht localPath danach selbst
                    return Task.CompletedTask;
                });

            var result = await serviceWithCustomSource.RunBackupAsync();

            result.Success.Should().BeTrue();
            var entries = await ListTarContentsAsync(keptArchivePath!);
            entries.Should().Contain(e => e.Replace('\\', '/').EndsWith($"{Path.GetFileName(customSourceDir)}/marker.txt"));
        }
        finally
        {
            if (keptArchivePath != null && File.Exists(keptArchivePath)) File.Delete(keptArchivePath);
            Directory.Delete(customSourceDir, true);
        }
    }

    private static async Task<List<string>> ListTarContentsAsync(string archivePath)
    {
        var processInfo = new ProcessStartInfo("tar") { RedirectStandardOutput = true, UseShellExecute = false };
        processInfo.ArgumentList.Add("tzf");
        processInfo.ArgumentList.Add(archivePath);

        using var process = Process.Start(processInfo)!;
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        // Windows-tar.exe liefert CRLF-Zeilenenden - trimmen statt nur auf '\n' zu splitten.
        return output.Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToList();
    }

    // ─── Download ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task OpenBackupFileForDownloadAsync_StreamtInhaltUndLoeschtTempDateiBeimSchliessen()
    {
        var target = await SeedTargetAsync();
        string? tempPathUsed = null;
        _provider.Setup(p => p.DownloadAsync(It.IsAny<BackupTarget>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<BackupTarget, string, string, CancellationToken>((_, _, localPath, _) =>
            {
                tempPathUsed = localPath;
                File.WriteAllText(localPath, "archiv-inhalt");
                return Task.CompletedTask;
            });

        await using (var stream = await _service.OpenBackupFileForDownloadAsync(target.Id, "backup-2026-09-14-030000.tar.gz"))
        {
            using var reader = new StreamReader(stream);
            (await reader.ReadToEndAsync()).Should().Be("archiv-inhalt");
            File.Exists(tempPathUsed).Should().BeTrue();
        }

        File.Exists(tempPathUsed!).Should().BeFalse();
    }

    // ─── History / Listing ────────────────────────────────────────────────────

    [Fact]
    public async Task GetHistoryAsync_FiltertNachZielUndSortiertNeuesteZuerst()
    {
        var targetA = await SeedTargetAsync();
        var targetB = await SeedTargetAsync();
        _context.BackupHistory.AddRange(
            new BackupHistory { BackupTargetId = targetA.Id, StartedAt = DateTime.UtcNow.AddDays(-1), Status = BackupStatus.Success, FileName = "a1", Type = BackupType.Daily },
            new BackupHistory { BackupTargetId = targetA.Id, StartedAt = DateTime.UtcNow, Status = BackupStatus.Success, FileName = "a2", Type = BackupType.Daily },
            new BackupHistory { BackupTargetId = targetB.Id, StartedAt = DateTime.UtcNow, Status = BackupStatus.Success, FileName = "b1", Type = BackupType.Daily });
        await _context.SaveChangesAsync();

        var history = await _service.GetHistoryAsync(targetA.Id);

        history.Should().HaveCount(2);
        history.Should().OnlyContain(h => h.BackupTargetId == targetA.Id);
        history[0].FileName.Should().Be("a2");
    }

    [Fact]
    public async Task ListBackupsOnTargetAsync_DelegiertAnProviderMitEntschluesseltenZugangsdaten()
    {
        var target = await SeedTargetAsync();
        var expectedFiles = new List<BackupFileInfo> { new("backup-1.tar.gz", DateTime.UtcNow, 10, BackupType.Daily) };

        BackupTarget? received = null;
        _provider.Setup(p => p.ListFilesAsync(It.IsAny<BackupTarget>(), It.IsAny<CancellationToken>()))
            .Callback<BackupTarget, CancellationToken>((t, _) => received = t)
            .ReturnsAsync(expectedFiles);

        var files = await _service.ListBackupsOnTargetAsync(target.Id);

        files.Should().BeEquivalentTo(expectedFiles);
        received!.Password.Should().Be("geheim");
    }

    // ─── RestoreAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RestoreAsync_UnbekanntesZiel_GibtFehlerZurueck()
    {
        var result = await _service.RestoreAsync(9999, "backup-2026-09-14-030000.tar.gz");

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Backup-Ziel nicht gefunden");
    }

    [Fact]
    public async Task RestoreAsync_VerschluesseltesBackupOhneKonfiguriertesPasswort_GibtFehlerZurueck()
    {
        var target = await SeedTargetAsync();
        _provider.Setup(p => p.DownloadAsync(It.IsAny<BackupTarget>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<BackupTarget, string, string, CancellationToken>((_, _, localPath, _) =>
            {
                File.WriteAllBytes(localPath, new byte[] { 1, 2, 3 });
                return Task.CompletedTask;
            });

        var result = await _service.RestoreAsync(target.Id, "backup-2026-09-14-030000.tar.gz.enc");

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Verschlüsseltes Backup, aber kein Passwort konfiguriert");
    }

    [Fact]
    public async Task RestoreAsync_EntpacktArchivInDenDatenordnerUndRaeumtAuf()
    {
        var target = await SeedTargetAsync();
        var testArchivePath = Path.Combine(Path.GetTempPath(), "restore-test-" + Guid.NewGuid() + ".tar.gz");
        await CreateArchiveWithDataFolderAsync(testArchivePath, "restored.txt", "aus dem backup");

        try
        {
            _provider.Setup(p => p.DownloadAsync(It.IsAny<BackupTarget>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns<BackupTarget, string, string, CancellationToken>((_, _, localPath, _) =>
                {
                    File.Copy(testArchivePath, localPath, overwrite: true);
                    return Task.CompletedTask;
                });

            var result = await _service.RestoreAsync(target.Id, "backup-2026-09-14-030000.tar.gz");

            result.Success.Should().BeTrue();
            File.ReadAllText(Path.Combine(_contentRoot, "data", "restored.txt")).Should().Be("aus dem backup");
            // Kein Sicherungsordner darf übrig bleiben - weder als Geschwister von "data"
            // (alter Ansatz, scheiterte an EBUSY bei echten Docker-Mounts) noch als Unterordner.
            Directory.Exists(Path.Combine(_contentRoot, "data.pre-restore")).Should().BeFalse();
            Directory.GetDirectories(Path.Combine(_contentRoot, "data"), ".pre-restore-*").Should().BeEmpty();
        }
        finally
        {
            if (File.Exists(testArchivePath)) File.Delete(testArchivePath);
        }
    }

    [Fact]
    public async Task RestoreAsync_DefektesArchiv_StelltAlteDatenWiederHerUndRaeumtAuf()
    {
        var target = await SeedTargetAsync();
        var dataDir = Path.Combine(_contentRoot, "data");

        _provider.Setup(p => p.DownloadAsync(It.IsAny<BackupTarget>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<BackupTarget, string, string, CancellationToken>((_, _, localPath, _) =>
            {
                File.WriteAllBytes(localPath, new byte[] { 1, 2, 3 }); // kein gültiges tar.gz
                return Task.CompletedTask;
            });

        var result = await _service.RestoreAsync(target.Id, "backup-2026-09-14-030000.tar.gz");

        result.Success.Should().BeFalse();
        // Die ursprüngliche Datei (aus dem Test-Setup) muss unangetastet wieder da sein.
        File.ReadAllText(Path.Combine(dataDir, "dummy.txt")).Should().Be("content");
        Directory.GetDirectories(dataDir, ".pre-restore-*").Should().BeEmpty();
    }

    [Fact]
    public async Task RestoreAsync_VerschiebenScheitertMittendrin_RolltAllesZurueckOhneDatenverlust()
    {
        // Regressionstest für einen Bug, der den "keys"-Ordner gekostet hat: Wenn das
        // Beiseiteräumen der aktuellen Daten mittendrin fehlschlägt (z.B. fehlende
        // Berechtigung auf einem bestimmten Unterordner wie "postgres"), darf dabei NICHTS
        // verloren gehen. Seit dem Umstieg von Directory.Move (rename()) auf Kopieren+Löschen
        // ist die kritische Garantie nicht mehr "kein Ordner bleibt zurück", sondern "die Daten
        // existieren garantiert irgendwo, nie nur halb": schlägt der Lösch-Schritt nach
        // erfolgreichem Kopieren fehl, bleibt das Original UND eine Kopie liegen (Platz-
        // verschwendung, aber kein Verlust) statt dass die Daten zwischen den Ordnern verschwinden.
        var target = await SeedTargetAsync();
        var dataDir = Path.Combine(_contentRoot, "data");

        var lockedDir = Path.Combine(dataDir, "postgres");
        Directory.CreateDirectory(lockedDir);
        var lockedFilePath = Path.Combine(lockedDir, "wichtig.txt");
        await File.WriteAllTextAsync(lockedFilePath, "unverzichtbare Daten");

        _provider.Setup(p => p.DownloadAsync(It.IsAny<BackupTarget>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<BackupTarget, string, string, CancellationToken>((_, _, localPath, _) =>
            {
                File.WriteAllBytes(localPath, new byte[] { 1, 2, 3 });
                return Task.CompletedTask;
            });

        // Offener Handle ohne Delete-Share simuliert einen Lösch-Fehler nach erfolgreichem
        // Kopieren (steht hier stellvertretend für z.B. "Access denied" bei fremdem Dateibesitzer
        // wie Postgres' 0700-Datenordner).
        await using (new FileStream(lockedFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var firstAttempt = await _service.RestoreAsync(target.Id, "backup-2026-09-14-030000.tar.gz");
            firstAttempt.Success.Should().BeFalse();
        }

        // Kritisch: Der Originalinhalt ist so oder so noch da - im schlimmsten Fall doppelt
        // (dataDir behält ihn, weil das Löschen scheiterte), aber niemals weg.
        File.ReadAllText(Path.Combine(dataDir, "dummy.txt")).Should().Be("content");
        File.Exists(lockedFilePath).Should().BeTrue();
        File.ReadAllText(lockedFilePath).Should().Be("unverzichtbare Daten");

        // Zweiter Versuch (Sperre jetzt freigegeben, Archiv weiterhin ungültig) darf ebenfalls
        // nichts verlieren.
        var secondAttempt = await _service.RestoreAsync(target.Id, "backup-2026-09-14-030000.tar.gz");

        secondAttempt.Success.Should().BeFalse();
        File.ReadAllText(Path.Combine(dataDir, "dummy.txt")).Should().Be("content");
        File.Exists(lockedFilePath).Should().BeTrue();
        File.ReadAllText(lockedFilePath).Should().Be("unverzichtbare Daten");
    }

    private static async Task CreateArchiveWithDataFolderAsync(string archivePath, string innerFileName, string content)
    {
        var stagingRoot = Directory.CreateTempSubdirectory("werkbank-restore-stage-").FullName;
        try
        {
            var dataDir = Path.Combine(stagingRoot, "data");
            Directory.CreateDirectory(dataDir);
            await File.WriteAllTextAsync(Path.Combine(dataDir, innerFileName), content);

            var processInfo = new ProcessStartInfo("tar") { RedirectStandardError = true, UseShellExecute = false };
            processInfo.ArgumentList.Add("czf");
            processInfo.ArgumentList.Add(archivePath);
            processInfo.ArgumentList.Add("-C");
            processInfo.ArgumentList.Add(stagingRoot);
            processInfo.ArgumentList.Add("data");

            using var process = Process.Start(processInfo)!;
            await process.WaitForExitAsync();
        }
        finally
        {
            Directory.Delete(stagingRoot, true);
        }
    }
}
