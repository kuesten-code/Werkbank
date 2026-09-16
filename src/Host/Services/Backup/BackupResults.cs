using Kuestencode.Werkbank.Host.Models;

namespace Kuestencode.Werkbank.Host.Services.Backup;

public record BackupResult(bool Success, string? Error, List<TargetResult> TargetResults);

public record TargetResult(int TargetId, string TargetName, bool Success, string? Error, long? Bytes);

public record BackupFileInfo(string FileName, DateTime Date, long SizeBytes, BackupType Type);

public record RestoreResult(bool Success, string? Error);

public record BackupStatusInfo(
    DateTime? LastSuccessfulBackup,
    bool IsOverdue,
    int DaysSinceLastBackup,
    bool IsRunning);
