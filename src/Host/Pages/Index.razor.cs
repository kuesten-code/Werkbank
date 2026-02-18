using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kuestencode.Shared.Contracts.Host;
using Kuestencode.Shared.Contracts.Navigation;
using Kuestencode.Shared.UI.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Kuestencode.Werkbank.Host.Pages;

public partial class Index
{
    private IReadOnlyList<ModuleInfoDto> _modules = Array.Empty<ModuleInfoDto>();

    [Inject] private IModuleRegistry ModuleRegistry { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        // Check if user is Mitarbeiter and redirect to Rapport
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true &&
            UserRoleResolver.ResolveRole(user, UserRole.Mitarbeiter) == UserRole.Mitarbeiter)
        {
            // Redirect Mitarbeiter directly to Rapport
            NavigationManager.NavigateTo("/rapport", forceLoad: true);
            return;
        }

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
