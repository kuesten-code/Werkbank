using Kuestencode.Core.Enums;

namespace Kuestencode.Shared.UI.Components.Settings;

/// <summary>
/// Model für E-Mail-Einstellungen, verwendet von EmailSettingsEditor.
/// </summary>
public class EmailSettingsModel
{
    public EmailLayout Layout { get; set; } = EmailLayout.Klar;
    public string PrimaryColor { get; set; } = "#0F2A3D";
    public string AccentColor { get; set; } = "#3FA796";
    public string? Greeting { get; set; }
    public string? Closing { get; set; }
}
