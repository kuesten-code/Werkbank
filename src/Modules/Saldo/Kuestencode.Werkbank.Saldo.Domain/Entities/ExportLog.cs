using System.ComponentModel.DataAnnotations;
using Kuestencode.Werkbank.Saldo.Domain.Enums;

namespace Kuestencode.Werkbank.Saldo.Domain.Entities;

/// <summary>
/// Protokolleintrag für einen durchgeführten Datenexport.
/// </summary>
public class ExportLog
{
    public Guid Id { get; set; }

    /// <summary>
    /// Art des Exports (DATEV-Buchungsstapel, Belege, PDF).
    /// </summary>
    public ExportTyp ExportTyp { get; set; }

    /// <summary>
    /// Erster Tag des exportierten Zeitraums.
    /// </summary>
    public DateOnly ZeitraumVon { get; set; }

    /// <summary>
    /// Letzter Tag des exportierten Zeitraums.
    /// </summary>
    public DateOnly ZeitraumBis { get; set; }

    /// <summary>
    /// Anzahl der exportierten Buchungen / Belege.
    /// </summary>
    public int AnzahlBuchungen { get; set; }

    /// <summary>
    /// Name der erzeugten Exportdatei.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string DateiName { get; set; } = string.Empty;

    /// <summary>
    /// Größe der Exportdatei in Bytes.
    /// </summary>
    public long DateiGroesse { get; set; }

    /// <summary>
    /// Zeitpunkt des Exports.
    /// </summary>
    public DateTime ExportedAt { get; set; }

    /// <summary>
    /// ID des Benutzers, der den Export durchgeführt hat.
    /// </summary>
    public Guid ExportedByUserId { get; set; }
}
