using System.Globalization;
using Kuestencode.Werkbank.Host.Models;

namespace Kuestencode.Werkbank.Host.Services.Backup;

/// <summary>
/// Namenskonvention der Backup-Dateien: backup-yyyy-MM-dd-HHmmss.tar.gz[.enc].
/// Wird von allen <see cref="IBackupTargetProvider"/>-Implementierungen zum Auflisten
/// vorhandener Backups verwendet.
/// </summary>
public static class BackupFileNaming
{
    private const string Prefix = "backup-";
    private const string DateFormat = "yyyy-MM-dd-HHmmss";

    public static string BuildFileName(DateTime timestampUtc) =>
        $"{Prefix}{timestampUtc.ToString(DateFormat, CultureInfo.InvariantCulture)}.tar.gz";

    public static bool IsBackupFile(string fileName) =>
        fileName.StartsWith(Prefix, StringComparison.Ordinal) && fileName.Contains(".tar.gz", StringComparison.Ordinal);

    public static DateTime ParseDateFromFileName(string fileName)
    {
        var namePart = System.IO.Path.GetFileName(fileName);
        var withoutPrefix = namePart.StartsWith(Prefix, StringComparison.Ordinal)
            ? namePart[Prefix.Length..]
            : namePart;

        var tarGzIndex = withoutPrefix.IndexOf(".tar.gz", StringComparison.Ordinal);
        var datePart = tarGzIndex >= 0 ? withoutPrefix[..tarGzIndex] : withoutPrefix;

        return DateTime.TryParseExact(datePart, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed)
            ? parsed
            : DateTime.MinValue;
    }

    public static BackupType DetermineBackupType(DateTime date)
    {
        if (date.Month == 1 && date.Day == 1)
            return BackupType.Yearly;
        if (date.Day == 1)
            return BackupType.Monthly;
        if (date.DayOfWeek == DayOfWeek.Sunday)
            return BackupType.Weekly;
        return BackupType.Daily;
    }
}
