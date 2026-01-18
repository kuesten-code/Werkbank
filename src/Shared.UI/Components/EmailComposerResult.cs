namespace Kuestencode.Shared.UI.Components;

/// <summary>
/// Result from the EmailComposer dialog.
/// </summary>
public class EmailComposerResult
{
    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? CcEmails { get; set; }
    public string? BccEmails { get; set; }
}
