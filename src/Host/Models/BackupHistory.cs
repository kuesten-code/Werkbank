using Kuestencode.Core.Models;

namespace Kuestencode.Werkbank.Host.Models;

public class BackupHistory : BaseEntity
{
    public int BackupTargetId { get; set; }

    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public BackupStatus Status { get; set; }
    public string? ErrorMessage { get; set; }

    public string FileName { get; set; } = "";
    public long? FileSizeBytes { get; set; }

    public BackupType Type { get; set; }
    public bool IsManual { get; set; } = false;

    public BackupTarget Target { get; set; } = null!;
}
