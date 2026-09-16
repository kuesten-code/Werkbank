using FluentAssertions;
using Kuestencode.Werkbank.Host.Models;
using Kuestencode.Werkbank.Host.Services.Backup.Providers;
using Xunit;

namespace Kuestencode.Host.Tests.Services.Backup;

public class LocalBackupProviderTests : IDisposable
{
    private readonly string _sourceDir;
    private readonly string _targetDir;
    private readonly LocalBackupProvider _provider = new();

    public LocalBackupProviderTests()
    {
        _sourceDir = Directory.CreateTempSubdirectory("werkbank-backup-src-").FullName;
        _targetDir = Path.Combine(Path.GetTempPath(), "werkbank-backup-target-" + Guid.NewGuid());
    }

    public void Dispose()
    {
        if (Directory.Exists(_sourceDir)) Directory.Delete(_sourceDir, true);
        if (Directory.Exists(_targetDir)) Directory.Delete(_targetDir, true);
    }

    private BackupTarget MakeTarget() => new() { Name = "Lokal", Type = BackupTargetType.Local, Path = _targetDir };

    [Fact]
    public async Task UploadAsync_LegtZielordnerAnUndKopiertDatei()
    {
        var localFile = Path.Combine(_sourceDir, "backup-2026-09-14-030000.tar.gz");
        await File.WriteAllTextAsync(localFile, "dummy-content");

        await _provider.UploadAsync(MakeTarget(), localFile, "backup-2026-09-14-030000.tar.gz", CancellationToken.None);

        File.Exists(Path.Combine(_targetDir, "backup-2026-09-14-030000.tar.gz")).Should().BeTrue();
    }

    [Fact]
    public async Task ListFilesAsync_FiltertAufBackupDateienUndOrdnetBackupTypeZu()
    {
        Directory.CreateDirectory(_targetDir);
        await File.WriteAllTextAsync(Path.Combine(_targetDir, "backup-2026-09-14-030000.tar.gz"), "x");
        await File.WriteAllTextAsync(Path.Combine(_targetDir, "readme.txt"), "x");

        var files = await _provider.ListFilesAsync(MakeTarget(), CancellationToken.None);

        files.Should().ContainSingle();
        files[0].FileName.Should().Be("backup-2026-09-14-030000.tar.gz");
        files[0].Type.Should().Be(BackupType.Daily);
    }

    [Fact]
    public async Task ListFilesAsync_GibtLeereListeZurueckWennOrdnerNichtExistiert()
    {
        var files = await _provider.ListFilesAsync(MakeTarget(), CancellationToken.None);

        files.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_EntferntDatei()
    {
        Directory.CreateDirectory(_targetDir);
        var filePath = Path.Combine(_targetDir, "backup-2026-09-14-030000.tar.gz");
        await File.WriteAllTextAsync(filePath, "x");

        await _provider.DeleteAsync(MakeTarget(), "backup-2026-09-14-030000.tar.gz", CancellationToken.None);

        File.Exists(filePath).Should().BeFalse();
    }

    [Fact]
    public async Task TestConnectionAsync_LegtOrdnerAnUndMeldetErfolg()
    {
        var (success, error) = await _provider.TestConnectionAsync(MakeTarget(), CancellationToken.None);

        success.Should().BeTrue();
        error.Should().BeNull();
        Directory.Exists(_targetDir).Should().BeTrue();
    }

    [Fact]
    public async Task TestConnectionAsync_MeldetFehlschlagOhnePfad()
    {
        var target = new BackupTarget { Name = "Lokal", Type = BackupTargetType.Local, Path = null };

        var (success, error) = await _provider.TestConnectionAsync(target, CancellationToken.None);

        success.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }
}
