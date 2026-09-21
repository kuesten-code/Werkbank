using Kuestencode.Shared.UI.Components;
using Kuestencode.Werkbank.Host.Services.Docker;
using MudBlazor;

namespace Kuestencode.Werkbank.Host.Pages.Settings;

public partial class Modulsteuerung
{
    private List<ModuleStatus> _modules = new();
    private bool _loading = true;
    private string? _errorMessage;
    private string? _busyKey;

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _loading = true;
        _errorMessage = null;
        try
        {
            _modules = await ModuleControl.GetStatusAsync();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Docker-Steuerung nicht erreichbar: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task StopAsync(ModuleStatus status)
    {
        var warnings = ModuleControlService.GetStopWarnings(status.Module.Key, _modules);
        var text = $"Modul '{status.Module.DisplayName}' wirklich stoppen?";
        if (warnings.Count > 0)
            text += " " + string.Join(" ", warnings);

        var parameters = new DialogParameters<ConfirmDialog>
        {
            { x => x.ContentText, text },
            { x => x.ConfirmButtonText, "Stoppen" },
            { x => x.ConfirmButtonColor, Color.Warning }
        };

        var dialog = await DialogService.ShowAsync<ConfirmDialog>("Modul stoppen", parameters);
        var result = await dialog.Result;

        if (result == null || result.Canceled)
            return;

        await RunAsync(status.Module.Key, () => ModuleControl.StopAsync(status.Module.Key),
            $"{status.Module.DisplayName} gestoppt.");
    }

    private Task StartAsync(ModuleStatus status) =>
        RunAsync(status.Module.Key, () => ModuleControl.StartAsync(status.Module.Key),
            $"{status.Module.DisplayName} gestartet.");

    private async Task RunAsync(string moduleKey, Func<Task> action, string successMessage)
    {
        _busyKey = moduleKey;
        try
        {
            await action();
            Snackbar.Add(successMessage, Severity.Success);
            _modules = await ModuleControl.GetStatusAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally
        {
            _busyKey = null;
        }
    }
}
