namespace Kuestencode.Shared.Contracts.Navigation;

public record NavItemDto
{
    public string Label { get; init; } = string.Empty;
    public string Href { get; init; } = string.Empty;
    public string? Icon { get; init; }
    public NavItemType Type { get; init; } = NavItemType.Link;
    public List<NavItemDto> Children { get; init; } = [];
}

public enum NavItemType
{
    Link,
    Group,
    Divider
}
