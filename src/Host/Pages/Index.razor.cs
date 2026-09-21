using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kuestencode.Core.Interfaces;
using Kuestencode.Shared.Contracts.Host;
using Kuestencode.Shared.Contracts.Navigation;
using Kuestencode.Shared.UI.Services;
using Kuestencode.Werkbank.Host.Models;
using Kuestencode.Werkbank.Host.Services;
using Kuestencode.Werkbank.Host.Services.Backup;
using Kuestencode.Werkbank.Host.Services.Docker;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using MudBlazor;

namespace Kuestencode.Werkbank.Host.Pages;

public record SystemStatusItem(string Name, bool IsHealthy, string? DetailMessage, string? NavigateTo = null);

public partial class Index : IDisposable
{
    private IReadOnlyList<ModuleInfoDto> _modules = Array.Empty<ModuleInfoDto>();

    private bool _loadingStatus = true;
    private List<SystemStatusItem> _configItems = new();
    private List<(string Name, ModuleDisplayState State)> _moduleStatus = new();

    private bool _isAdmin;
    private BackupStatusInfo? _backupStatus;
    private BackupHistory? _latestSuccessfulBackup;
    private bool _downloadingBackup;

    [Inject] private ModuleRegistry ModuleRegistry { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private ICompanyService CompanyService { get; set; } = default!;
    [Inject] private IBackupService BackupService { get; set; } = default!;
    [Inject] private IManuallyStoppedModules ManuallyStoppedModules { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

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

        _isAdmin = UserRoleResolver.ResolveRole(user, UserRole.Mitarbeiter) == UserRole.Admin;

        ModuleRegistry.OnChanged += HandleModulesChanged;
        RefreshModules();

        await LoadSystemstatusAsync();

        if (_isAdmin)
        {
            await LoadBackupNotificationAsync();
        }
    }

    private async Task LoadBackupNotificationAsync()
    {
        try
        {
            _backupStatus = await BackupService.GetStatusAsync();
            var history = await BackupService.GetHistoryAsync(limit: 5);
            _latestSuccessfulBackup = history.FirstOrDefault(h => h.Status == BackupStatus.Success);
        }
        catch
        {
            // Backup-Status ist für die Startseite nicht kritisch - Banner bleibt einfach aus.
        }
    }

    private async Task DownloadLatestBackupAsync()
    {
        if (_latestSuccessfulBackup == null)
            return;

        _downloadingBackup = true;
        try
        {
            await using var stream = await BackupService.OpenBackupFileForDownloadAsync(
                _latestSuccessfulBackup.BackupTargetId, _latestSuccessfulBackup.FileName);
            using var streamRef = new DotNetStreamReference(stream, leaveOpen: false);
            await JSRuntime.InvokeVoidAsync("downloadFileFromStream", _latestSuccessfulBackup.FileName, streamRef);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Download fehlgeschlagen: {ex.Message}", Severity.Error);
        }
        finally
        {
            _downloadingBackup = false;
        }
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
        _moduleStatus = ModuleStatusResolver.Resolve(ModuleRegistry.GetAllModulesWithStatus(), ManuallyStoppedModules);
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
