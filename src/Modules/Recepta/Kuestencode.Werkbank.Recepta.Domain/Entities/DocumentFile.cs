using System.ComponentModel.DataAnnotations;

namespace Kuestencode.Werkbank.Recepta.Domain.Entities;

/// <summary>
/// Ein Dateianhang zu einem Beleg.
/// </summary>
public class DocumentFile
{
    public Guid Id { get; set; }

    /// <summary>
    /// Referenz zum Beleg.
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Originaler Dateiname.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// MIME-Typ der Datei.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Dateigröße in Bytes.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Pfad zur gespeicherten Datei.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string StoragePath { get; set; } = string.Empty;

    // Timestamps
    public DateTime CreatedAt { get; set; }

    // Navigation Properties
    public Document Document { get; set; } = null!;
}
