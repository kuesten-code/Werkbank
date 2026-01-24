using Kuestencode.Shared.Contracts.Navigation;

namespace Kuestencode.Werkbank.Host.Services;

public interface IHostNavigationService
{
    List<NavItemDto> GetNavigationItems();
}

public class HostNavigationService : IHostNavigationService
{
    private readonly IModuleRegistry _moduleRegistry;

    public HostNavigationService(IModuleRegistry moduleRegistry)
    {
        _moduleRegistry = moduleRegistry;
    }

    public List<NavItemDto> GetNavigationItems()
    {
        var items = new List<NavItemDto>
        {
            new NavItemDto
            {
                Label = "Übersicht",
                Href = "/",
                Icon = "Dashboard",
                Type = NavItemType.Link
            },
            new NavItemDto
            {
                Label = "Kunden",
                Href = "/customers",
                Icon = "People",
                Type = NavItemType.Link
            },
            new NavItemDto
            {
                Label = "Einstellungen",
                Icon = "Settings",
                Type = NavItemType.Group,
                Children = new List<NavItemDto>
                {
                    new NavItemDto
                    {
                        Label = "Firmendaten",
                        Href = "/settings/company",
                        Icon = "Business",
                        Type = NavItemType.Link
                    },
                    new NavItemDto
                    {
                        Label = "Email Versand",
                        Href = "/settings/email",
                        Icon = "Email",
                        Type = NavItemType.Link
                    }
                }
            }
        };

        var modules = _moduleRegistry.GetAllModules();
        foreach (var module in modules)
        {
            if (module.NavigationItems.Count == 0)
            {
                continue;
            }

            items.Add(new NavItemDto { Type = NavItemType.Divider });
            items.AddRange(module.NavigationItems);
        }

        return items;
    }
}
