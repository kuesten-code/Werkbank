using System.ComponentModel.DataAnnotations;
using Kuestencode.Core.Enums;

namespace Kuestencode.Rapport.Models;

/// <summary>
/// Persistent settings for the Rapport module.
/// </summary>
public class RapportSettings
{
    public int Id { get; set; } = 1;

    /// <summary>
    /// Default hourly rate used in PDF exports.
    /// </summary>
    [Range(0, 1000000)]
    public decimal DefaultHourlyRate { get; set; } = 0m;

    /// <summary>
    /// Whether to show the hourly rate in the PDF.
    /// </summary>
    public bool ShowHourlyRateInPdf { get; set; } = false;

    /// <summary>
    /// Whether to calculate and show the total amount in the PDF.
    /// Requires hourly rate.
    /// </summary>
    public bool CalculateTotalAmount { get; set; } = false;

    /// <summary>
    /// Rounding interval in minutes (0/5/15/30).
    /// </summary>
    public int RoundingMinutes { get; set; } = 0;

    /// <summary>
    /// Start of week for weekly summaries.
    /// </summary>
    public DayOfWeek StartOfWeek { get; set; } = DayOfWeek.Monday;

    /// <summary>
    /// Default project for new entries.
    /// </summary>
    public int? DefaultProjectId { get; set; }

    /// <summary>
    /// Auto stop running timers after X hours.
    /// </summary>
    public int? AutoStopTimerAfterHours { get; set; }

    /// <summary>
    /// Enables UI sounds.
    /// </summary>
    public bool EnableSounds { get; set; } = false;

    /// <summary>
    /// PDF layout style.
    /// </summary>
    public PdfLayout PdfLayout { get; set; } = PdfLayout.Klar;

    /// <summary>
    /// Primary PDF color.
    /// </summary>
    [MaxLength(20)]
    public string PdfPrimaryColor { get; set; } = "#1f3a5f";

    /// <summary>
    /// Accent PDF color.
    /// </summary>
    [MaxLength(20)]
    public string PdfAccentColor { get; set; } = "#3FA796";

    /// <summary>
    /// Optional header text for PDF exports.
    /// </summary>
    [MaxLength(500)]
    public string? PdfHeaderText { get; set; }

    /// <summary>
    /// Optional footer text for PDF exports.
    /// </summary>
    [MaxLength(1000)]
    public string? PdfFooterText { get; set; }

    public static RapportSettings CreateDefault()
    {
        return new RapportSettings
        {
            Id = 1,
            DefaultHourlyRate = 0m,
            ShowHourlyRateInPdf = false,
            CalculateTotalAmount = false,
            RoundingMinutes = 0,
            StartOfWeek = DayOfWeek.Monday,
            DefaultProjectId = null,
            AutoStopTimerAfterHours = null,
            EnableSounds = false,
            PdfLayout = PdfLayout.Klar,
            PdfPrimaryColor = "#1f3a5f",
            PdfAccentColor = "#3FA796",
            PdfHeaderText = null,
            PdfFooterText = null
        };
    }
}
