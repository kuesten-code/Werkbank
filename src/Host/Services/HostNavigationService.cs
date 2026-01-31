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
                Label = "Einstellungen",
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

        foreach (var entry in moduleOrder)
        {
            var module = entry.Module;
            if (module.NavigationItems.Count == 0)
            {
                continue;
            }

            items.Add(new NavItemDto { Type = NavItemType.Divider });
            items.AddRange(module.NavigationItems);
        }

        return items;
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
