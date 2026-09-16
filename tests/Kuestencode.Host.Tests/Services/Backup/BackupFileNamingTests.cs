using FluentAssertions;
using Kuestencode.Werkbank.Host.Models;
using Kuestencode.Werkbank.Host.Services.Backup;
using Xunit;

namespace Kuestencode.Host.Tests.Services.Backup;

public class BackupFileNamingTests
{
    [Fact]
    public void BuildFileName_ErzeugtErwartetesFormat()
    {
        var timestamp = new DateTime(2026, 9, 14, 3, 0, 0, DateTimeKind.Utc);

        var fileName = BackupFileNaming.BuildFileName(timestamp);

        fileName.Should().Be("backup-2026-09-14-030000.tar.gz");
    }

    [Theory]
    [InlineData("backup-2026-09-14-030000.tar.gz")]
    [InlineData("backup-2026-09-14-030000.tar.gz.enc")]
    public void ParseDateFromFileName_LiestZeitstempelUnabhaengigVonEncSuffix(string fileName)
    {
        var date = BackupFileNaming.ParseDateFromFileName(fileName);

        date.Should().Be(new DateTime(2026, 9, 14, 3, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void IsBackupFile_ErkenntNurBackupDateien()
    {
        BackupFileNaming.IsBackupFile("backup-2026-09-14-030000.tar.gz").Should().BeTrue();
        BackupFileNaming.IsBackupFile("backup-2026-09-14-030000.tar.gz.enc").Should().BeTrue();
        BackupFileNaming.IsBackupFile("readme.txt").Should().BeFalse();
        BackupFileNaming.IsBackupFile("other-2026-09-14.tar.gz").Should().BeFalse();
    }

    [Theory]
    [InlineData(2026, 1, 1, BackupType.Yearly)]   // Neujahr hat Vorrang vor Monats-/Wochenanfang
    [InlineData(2026, 4, 1, BackupType.Monthly)]  // Monatserster (kein Sonntag)
    [InlineData(2026, 9, 13, BackupType.Weekly)]  // Sonntag
    [InlineData(2026, 9, 14, BackupType.Daily)]   // gewöhnlicher Werktag
    public void DetermineBackupType_WendetPrioritaetJaehrlichVorMonatlichVorWoechentlichAn(
        int year, int month, int day, BackupType expected)
    {
        var date = new DateTime(year, month, day, 3, 0, 0, DateTimeKind.Utc);

        BackupFileNaming.DetermineBackupType(date).Should().Be(expected);
    }
}
