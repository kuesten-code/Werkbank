using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Kuestencode.Shared.Contracts.Navigation;

namespace Kuestencode.Werkbank.Host.Pages;

public partial class Index
{
    private IReadOnlyList<ModuleInfoDto> _modules = Array.Empty<ModuleInfoDto>();

    [Inject] private IModuleRegistry ModuleRegistry { get; set; } = default!;

    protected override void OnInitialized()
    {
        _modules = ModuleRegistry.GetAllModules()
            .OrderBy(m => m.DisplayName)
            .ToList();
    }

    private static string GetModuleHref(ModuleInfoDto module)
    {
        return module.NavigationItems
            .FirstOrDefault(item => item.Type == NavItemType.Link && !string.IsNullOrWhiteSpace(item.Href))
            ?.Href ?? string.Empty;
    }

}
