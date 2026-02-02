using System.ComponentModel.DataAnnotations;
using Kuestencode.Core.Enums;

namespace Kuestencode.Werkbank.Offerte.Domain.Entities;

/// <summary>
/// Einstellungen für das Offerte-Modul (E-Mail und PDF Design).
/// Es gibt nur eine Instanz dieser Einstellungen pro Installation.
/// </summary>
public class OfferteSettings
{
    public int Id { get; set; }

    // === Email Design Settings ===

    public EmailLayout EmailLayout { get; set; } = EmailLayout.Klar;

    [MaxLength(7)]
    public string EmailPrimaryColor { get; set; } = "#0F2A3D";

    [MaxLength(7)]
    public string EmailAccentColor { get; set; } = "#3FA796";

    [MaxLength(500)]
    public string? EmailGreeting { get; set; }

    [MaxLength(500)]
    public string? EmailClosing { get; set; }

    // === PDF Design Settings ===

    public PdfLayout PdfLayout { get; set; } = PdfLayout.Klar;

    [MaxLength(7)]
    public string PdfPrimaryColor { get; set; } = "#1f3a5f";

    [MaxLength(7)]
    public string PdfAccentColor { get; set; } = "#3FA796";

    [MaxLength(500)]
    public string? PdfHeaderText { get; set; }

    [MaxLength(1000)]
    public string? PdfFooterText { get; set; }

    [MaxLength(500)]
    public string? PdfValidityNotice { get; set; }
}
