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
                Label = "Werkbank",
                Href = "/",
                Icon = "/company/logos/Werkbank_Logo.png",
                Type = NavItemType.Link
            },
            new NavItemDto
            {
                Label = "Kunden",
                Href = "/customers",
                Icon = "",
                Type = NavItemType.Link
            }
        };

        var modules = _moduleRegistry.GetAllModules();
        var moduleOrder = modules
            .Select((module, index) => new
            {
                Module = module,
                Index = index,
                Priority = GetModulePriority(module.ModuleName)
            })
            .OrderBy(entry => entry.Priority.HasValue ? 0 : 1)
            .ThenBy(entry => entry.Priority ?? int.MaxValue)
            .ThenBy(entry => entry.Priority.HasValue ? entry.Module.ModuleName : string.Empty)
            .ThenBy(entry => entry.Priority.HasValue ? 0 : entry.Index);

        // Collect all Settings items from modules
        var moduleSettingsItems = new List<NavItemDto>();

        foreach (var entry in moduleOrder)
        {
            var module = entry.Module;
            if (module.NavigationItems.Count == 0)
            {
                continue;
            }

            // Separate Settings items from regular navigation items
            var regularItems = module.NavigationItems.Where(item => item.Type != NavItemType.Settings).ToList();
            var settingsItems = module.NavigationItems.Where(item => item.Type == NavItemType.Settings).ToList();

            // Add regular navigation items
            if (regularItems.Count > 0)
            {
                items.Add(new NavItemDto { Type = NavItemType.Divider });
                items.AddRange(regularItems);
            }

            // Collect settings for aggregation
            moduleSettingsItems.AddRange(settingsItems);
        }

        // Build aggregated Einstellungen group
        var settingsGroup = BuildSettingsGroup(moduleSettingsItems);
        items.Add(new NavItemDto { Type = NavItemType.Divider });
        items.Add(settingsGroup);

        return items;
    }

    private static NavItemDto BuildSettingsGroup(List<NavItemDto> moduleSettingsItems)
    {
        var settingsChildren = new List<NavItemDto>
        {
            // Host settings first
            new NavItemDto
            {
                Label = "Werkbank",
                Icon = "",
                Type = NavItemType.Group,
                Children = new List<NavItemDto>
                {
                    new NavItemDto
                    {
                        Label = "Firmendaten",
                        Href = "/settings/company",
                        Icon = "",
                        Type = NavItemType.Link
                    },
                    new NavItemDto
                    {
                        Label = "Email Versand",
                        Href = "/settings/email",
                        Icon = "",
                        Type = NavItemType.Link
                    }
                }
            }
        };

        // Add module settings as subgroups
        foreach (var moduleSettings in moduleSettingsItems)
        {
            settingsChildren.Add(new NavItemDto
            {
                Label = moduleSettings.Label,
                Icon = moduleSettings.Icon,
                Type = NavItemType.Group,
                Children = moduleSettings.Children
            });
        }

        return new NavItemDto
        {
            Label = "Einstellungen",
            Icon = "",
            Type = NavItemType.Group,
            Children = settingsChildren
        };
    }

    private static int? GetModulePriority(string moduleName)
    {
        return moduleName switch
        {
            "Faktura" => 1,
            "Offerte" => 5,
            "Rapport" => 10,
            _ => null
        };
    }
}
