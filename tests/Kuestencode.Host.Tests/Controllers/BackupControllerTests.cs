using FluentAssertions;
using Kuestencode.Werkbank.Host.Controllers;
using Kuestencode.Werkbank.Host.Services.Backup;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Kuestencode.Host.Tests.Controllers;

public class BackupControllerTests
{
    private readonly Mock<IBackupService> _backupService = new();
    private readonly BackupController _controller;

    public BackupControllerTests()
    {
        _controller = new BackupController(_backupService.Object);
    }

    [Fact]
    public async Task Download_LiefertDateiStreamMitPassendemContentType()
    {
        var content = new MemoryStream(new byte[] { 1, 2, 3 });
        _backupService.Setup(s => s.OpenBackupFileForDownloadAsync(1, "backup-2026-09-14-030000.tar.gz"))
            .ReturnsAsync(content);

        var result = await _controller.Download(1, "backup-2026-09-14-030000.tar.gz");

        var fileResult = result.Should().BeOfType<FileStreamResult>().Subject;
        fileResult.ContentType.Should().Be("application/gzip");
        fileResult.FileDownloadName.Should().Be("backup-2026-09-14-030000.tar.gz");
    }

    [Fact]
    public async Task Download_UnbekanntesZiel_GibtNotFoundZurueck()
    {
        _backupService.Setup(s => s.OpenBackupFileForDownloadAsync(It.IsAny<int>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Backup-Ziel 99 nicht gefunden"));

        var result = await _controller.Download(99, "backup.tar.gz");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Download_UnerwarteterFehler_Gibt500Zurueck()
    {
        _backupService.Setup(s => s.OpenBackupFileForDownloadAsync(It.IsAny<int>(), It.IsAny<string>()))
            .ThrowsAsync(new IOException("Netzwerkfehler"));

        var result = await _controller.Download(1, "backup.tar.gz");

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }
}
