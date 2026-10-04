using Kuestencode.Core.Models;

namespace Kuestencode.Werkbank.Host.Models.Feedback;

/// <summary>
/// Konfiguration des Feedback-Systems. Es existiert genau eine Zeile (Singleton-Settings,
/// analog zu <see cref="BackupSettings"/>).
/// </summary>
public class FeedbackSettings : BaseEntity
{
    public FeedbackRole Role { get; set; } = FeedbackRole.Aus;

    // Rolle Client
    public string? HubUrl { get; set; }

    /// <summary>Verschlüsselt via <see cref="Services.PasswordEncryptionService"/> gespeichert.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Stand des letzten erfolgreichen Pollings (Hub-Zeit), Grundlage für "since".</summary>
    public DateTime? LastSyncAt { get; set; }

    // Rolle Hub
    public bool NotifyOnNewReport { get; set; }
    public string? NotificationEmail { get; set; }
    public int MaxFileSizeMb { get; set; } = 5;
    public int MaxReportSizeMb { get; set; } = 20;
}
