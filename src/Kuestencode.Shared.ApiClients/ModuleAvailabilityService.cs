using Kuestencode.Shared.Contracts.Navigation;

namespace Kuestencode.Shared.ApiClients;

public class ModuleAvailabilityService(IHostApiClient hostApiClient)
{
    public async Task<ModuleAvailability> CheckAsync()
    {
        try
        {
            var navItems = await hostApiClient.GetNavigationAsync();
            return new ModuleAvailability(
                Rapport:  navItems.Any(n => IsModule(n, "/rapport")),
                Acta:     navItems.Any(n => IsModule(n, "/acta")),
                Recepta:  navItems.Any(n => IsModule(n, "/recepta")),
                Faktura:  navItems.Any(n => IsModule(n, "/faktura")),
                Offerte:  navItems.Any(n => IsModule(n, "/angebote"))
            );
        }
        catch
        {
            return ModuleAvailability.None;
        }
    }

    private static bool IsModule(NavItemDto item, string prefix)
    {
        if (!string.IsNullOrWhiteSpace(item.Href) &&
            item.Href.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return true;

        return item.Children is { Count: > 0 } &&
               item.Children.Any(c => IsModule(c, prefix));
    }
}

public record ModuleAvailability(
    bool Rapport,
    bool Acta,
    bool Recepta,
    bool Faktura,
    bool Offerte)
{
    public static readonly ModuleAvailability None = new(false, false, false, false, false);
}
