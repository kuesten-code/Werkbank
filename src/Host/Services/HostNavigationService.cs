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
            },
            new NavItemDto
            {
                Label = "Mitarbeiter",
                Href = "/team-members",
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
        // Group settings by category
        var settingsByCategory = new Dictionary<NavSettingsCategory, List<NavItemDto>>
        {
            { NavSettingsCategory.Allgemein, new List<NavItemDto>() },
            { NavSettingsCategory.Vorlagen, new List<NavItemDto>() },
            { NavSettingsCategory.Dokumente, new List<NavItemDto>() },
            { NavSettingsCategory.Versand, new List<NavItemDto>() },
            { NavSettingsCategory.Abrechnung, new List<NavItemDto>() }
        };

        // Add Host settings to "Allgemein"
        settingsByCategory[NavSettingsCategory.Allgemein].Add(new NavItemDto
        {
            Label = "Firmendaten",
            Href = "/settings/company",
            Icon = "",
            Type = NavItemType.Link
        });



        // Add Host email settings to "Versand"
        settingsByCategory[NavSettingsCategory.Versand].Add(new NavItemDto
        {
            Label = "SMTP-Server",
            Href = "/settings/email",
            Icon = "",
            Type = NavItemType.Link
        });

        // Distribute module settings by category
        foreach (var moduleSettings in moduleSettingsItems)
        {
            var category = moduleSettings.Category ?? NavSettingsCategory.Allgemein;

            if (!string.IsNullOrEmpty(moduleSettings.Href))
            {
                // Direct link
                settingsByCategory[category].Add(new NavItemDto
                {
                    Label = moduleSettings.Label,
                    Href = moduleSettings.Href,
                    Icon = moduleSettings.Icon,
                    Type = NavItemType.Link
                });
            }
            else if (moduleSettings.Children.Count > 0)
            {
                // Multiple children - add them all to the category
                settingsByCategory[category].AddRange(moduleSettings.Children);
            }
        }

        // Build category groups (only include non-empty categories)
        var settingsChildren = new List<NavItemDto>();

        var categoryLabels = new Dictionary<NavSettingsCategory, string>
        {
            { NavSettingsCategory.Allgemein, "Allgemein" },
            { NavSettingsCategory.Vorlagen, "Vorlagen" },
            { NavSettingsCategory.Dokumente, "Dokumente" },
            { NavSettingsCategory.Versand, "Versand" },
            { NavSettingsCategory.Abrechnung, "Abrechnung" }
        };

        foreach (var (category, items) in settingsByCategory)
        {
            if (items.Count == 0) continue;

            // If only one item, show it directly without a subgroup
            if (items.Count == 1)
            {
                settingsChildren.Add(new NavItemDto
                {
                    Label = categoryLabels[category],
                    Href = items[0].Href,
                    Icon = "",
                    Type = NavItemType.Link
                });
            }
            else
            {
                settingsChildren.Add(new NavItemDto
                {
                    Label = categoryLabels[category],
                    Icon = "",
                    Type = NavItemType.Group,
                    Children = items
                });
            }
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
