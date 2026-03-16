using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kuestencode.Core.Interfaces;
using Kuestencode.Shared.Contracts.Host;
using Kuestencode.Shared.Contracts.Navigation;
using Kuestencode.Shared.UI.Services;
using Kuestencode.Werkbank.Host.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Kuestencode.Werkbank.Host.Pages;

public record SystemStatusItem(string Name, bool IsHealthy, string? DetailMessage, string? NavigateTo = null);

public partial class Index : IDisposable
{
    private IReadOnlyList<ModuleInfoDto> _modules = Array.Empty<ModuleInfoDto>();

    private bool _loadingStatus = true;
    private List<SystemStatusItem> _configItems = new();
    private List<(string Name, bool Online)> _moduleStatus = new();

    [Inject] private ModuleRegistry ModuleRegistry { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private ICompanyService CompanyService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true &&
            UserRoleResolver.ResolveRole(user, UserRole.Mitarbeiter) == UserRole.Mitarbeiter)
        {
            NavigationManager.NavigateTo("/rapport", forceLoad: true);
            return;
        }

        ModuleRegistry.OnChanged += HandleModulesChanged;
        RefreshModules();

        await LoadSystemstatusAsync();
    }

    private async Task LoadSystemstatusAsync()
    {
        _loadingStatus = true;

        try
        {
            // Config checks
            var configItems = new List<SystemStatusItem>();

            bool hasCompany;
            try { hasCompany = await CompanyService.HasCompanyDataAsync(); }
            catch { hasCompany = false; }

            configItems.Add(new SystemStatusItem(
                "Firmenstammdaten",
                hasCompany,
                hasCompany ? null : "Bitte vervollständigen Sie Ihre Firmenstammdaten.",
                "/settings/company"));

            bool hasEmail;
            try { hasEmail = await CompanyService.IsEmailConfiguredAsync(); }
            catch { hasEmail = false; }

            configItems.Add(new SystemStatusItem(
                "E-Mail-Versand",
                hasEmail,
                hasEmail ? null : "E-Mail-Versand ist noch nicht eingerichtet.",
                "/settings/email"));

            _configItems = configItems;

            RefreshModuleStatus();
        }
        finally
        {
            _loadingStatus = false;
        }
    }

    private void RefreshModules()
    {
        _modules = ModuleRegistry.GetAllModules()
            .OrderBy(m => m.DisplayName)
            .ToList();
    }

    private void HandleModulesChanged()
    {
        InvokeAsync(() =>
        {
            RefreshModules();
            RefreshModuleStatus();
            StateHasChanged();
        });
    }

    private void RefreshModuleStatus()
    {
        _moduleStatus = ModuleRegistry.GetAllModulesWithStatus()
            .OrderBy(x => x.Module.DisplayName)
            .Select(x => (x.Module.DisplayName, x.IsOnline))
            .ToList();
    }

    public void Dispose()
    {
        ModuleRegistry.OnChanged -= HandleModulesChanged;
    }

    private static string GetModuleHref(ModuleInfoDto module)
    {
        return module.NavigationItems
            .FirstOrDefault(item => item.Type == NavItemType.Link && !string.IsNullOrWhiteSpace(item.Href))
            ?.Href ?? string.Empty;
    }
}
