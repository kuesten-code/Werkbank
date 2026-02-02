namespace Kuestencode.Shared.Contracts.Navigation;

public record NavItemDto
{
    public string Label { get; init; } = string.Empty;
    public string Href { get; init; } = string.Empty;
    public string? Icon { get; init; }
    public NavItemType Type { get; init; } = NavItemType.Link;
    public List<NavItemDto> Children { get; init; } = [];

    /// <summary>
    /// Category for Settings items. Used by the Host to group settings by task.
    /// </summary>
    public NavSettingsCategory? Category { get; init; }
}

public enum NavItemType
{
    Link,
    Group,
    Divider,
    /// <summary>
    /// Settings items are aggregated by the Host into a central "Einstellungen" group.
    /// Modules should provide Settings items with Category and either:
    /// - Href for a direct link, or
    /// - Children for multiple settings links
    /// </summary>
    Settings
}

/// <summary>
/// Categories for grouping settings by task/function.
/// </summary>
public enum NavSettingsCategory
{
    /// <summary>General/Core settings (Firmendaten, etc.)</summary>
    Allgemein = 0,
    /// <summary>Template settings (Email templates, etc.)</summary>
    Vorlagen = 1,
    /// <summary>Document settings (PDF layouts, etc.)</summary>
    Dokumente = 2,
    /// <summary>Sending/Email settings</summary>
    Versand = 3,
    /// <summary>Billing/Invoice settings</summary>
    Abrechnung = 4
}
