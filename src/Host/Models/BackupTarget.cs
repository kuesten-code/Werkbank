using Kuestencode.Core.Models;

namespace Kuestencode.Werkbank.Host.Models;

/// <summary>
/// Ein konfiguriertes Backup-Ziel (lokal, SFTP, S3 oder WebDAV). Mehrere Ziele können
/// parallel aktiv sein — jedes Backup wird zu allen aktiven Zielen hochgeladen.
/// </summary>
public class BackupTarget : BaseEntity
{
    public int BackupSettingsId { get; set; }

    public string Name { get; set; } = "";
    public BackupTargetType Type { get; set; }
    public bool Enabled { get; set; } = true;

    // Verbindungsdaten (je nach Type unterschiedlich genutzt)
    public string? Path { get; set; }
    public string? Host { get; set; }
    public int? Port { get; set; }

    /// <summary>Nur für WebDAV relevant: bestimmt, ob https:// oder http:// verwendet wird
    /// (z.B. für einen internen WebDAV-Server ohne eigenes TLS im selben Docker-Netzwerk).</summary>
    public bool UseHttps { get; set; } = true;

    public string? Username { get; set; }

    /// <summary>Verschlüsselt via <see cref="Services.PasswordEncryptionService"/> gespeichert.</summary>
    public string? Password { get; set; }

    /// <summary>Verschlüsselt via <see cref="Services.PasswordEncryptionService"/> gespeichert.</summary>
    public string? PrivateKey { get; set; }

    public string? AccessKey { get; set; }

    /// <summary>Verschlüsselt via <see cref="Services.PasswordEncryptionService"/> gespeichert.</summary>
    public string? SecretKey { get; set; }

    public string? Region { get; set; }

    public DateTime? LastBackupAt { get; set; }
    public bool? LastBackupSuccess { get; set; }
    public string? LastBackupError { get; set; }
}
