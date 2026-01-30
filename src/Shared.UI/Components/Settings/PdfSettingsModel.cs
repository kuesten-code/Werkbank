using Kuestencode.Core.Enums;

namespace Kuestencode.Shared.UI.Components.Settings;

/// <summary>
/// Model für PDF-Einstellungen, verwendet von PdfSettingsEditor.
/// </summary>
public class PdfSettingsModel
{
    public PdfLayout Layout { get; set; } = PdfLayout.Klar;
    public string PrimaryColor { get; set; } = "#1f3a5f";
    public string AccentColor { get; set; } = "#3FA796";
    public string? HeaderText { get; set; }
    public string? FooterText { get; set; }

    /// <summary>
    /// Für Rechnungen: Zahlungshinweis
    /// Für Angebote: Gültigkeitshinweis
    /// </summary>
    public string? NoticeText { get; set; }
}
