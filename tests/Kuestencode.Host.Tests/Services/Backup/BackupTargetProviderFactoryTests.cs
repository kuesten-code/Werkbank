using FluentAssertions;
using Kuestencode.Werkbank.Host.Models;
using Kuestencode.Werkbank.Host.Services.Backup;
using Kuestencode.Werkbank.Host.Services.Backup.Providers;
using Xunit;

namespace Kuestencode.Host.Tests.Services.Backup;

public class BackupTargetProviderFactoryTests
{
    private readonly BackupTargetProviderFactory _factory = new();

    [Theory]
    [InlineData(BackupTargetType.Local, typeof(LocalBackupProvider))]
    [InlineData(BackupTargetType.Sftp, typeof(SftpBackupProvider))]
    [InlineData(BackupTargetType.S3, typeof(S3BackupProvider))]
    [InlineData(BackupTargetType.WebDav, typeof(WebDavBackupProvider))]
    public void GetProvider_LiefertErwartetenProviderTyp(BackupTargetType type, Type expected)
    {
        _factory.GetProvider(type).Should().BeOfType(expected);
    }

    [Fact]
    public void GetProvider_WirftBeiUnbekanntemTyp()
    {
        var act = () => _factory.GetProvider((BackupTargetType)999);

        act.Should().Throw<NotSupportedException>();
    }
}
