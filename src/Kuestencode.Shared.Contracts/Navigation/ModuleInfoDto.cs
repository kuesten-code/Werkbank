namespace Kuestencode.Shared.Contracts.Navigation;

public record ModuleInfoDto
{
    public string ModuleName { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string? HealthCheckUrl { get; init; }
    public List<NavItemDto> NavigationItems { get; init; } = [];
}
