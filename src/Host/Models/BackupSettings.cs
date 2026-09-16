using Kuestencode.Core.Models;

namespace Kuestencode.Werkbank.Host.Models;

/// <summary>
/// Plattformweite Backup-Konfiguration. Es existiert genau eine Zeile (Singleton-Settings,
/// analog zu <see cref="WerkbankSettings"/>).
/// </summary>
public class BackupSettings : BaseEntity
{
    public string Schedule { get; set; } = "0 3 * * *";
    public bool Enabled { get; set; } = false;

    public bool EncryptionEnabled { get; set; } = false;

    /// <summary>Verschlüsselt via <see cref="Services.PasswordEncryptionService"/> gespeichert.</summary>
    public string? EncryptionPassword { get; set; }

    public int KeepDaily { get; set; } = 7;
    public int KeepWeekly { get; set; } = 4;
    public int KeepMonthly { get; set; } = 12;
    public int KeepYearly { get; set; } = 10;

    public bool AlertOnFailure { get; set; } = true;
    public string? AlertEmail { get; set; }
    public string? AlertWebhookUrl { get; set; }

    public int WarnAfterDays { get; set; } = 3;

    public List<BackupTarget> Targets { get; set; } = new();
}
