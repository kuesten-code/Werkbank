using Kuestencode.Shared.UI.Components;
using Kuestencode.Werkbank.Host.Models;
using Kuestencode.Werkbank.Host.Services.Backup;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Kuestencode.Werkbank.Host.Pages.Settings;

public partial class Backup
{
    private BackupSettings _settings = new();
    private List<BackupTarget> _targets = new();
    private List<BackupHistory> _history = new();
    private BackupStatusInfo? _status;

    private bool _loading = true;
    private bool _saving = false;
    private bool _runningManual = false;
    private string? _errorMessage;

    private bool _hasStoredEncryptionPassword;
    private string _encryptionPasswordInput = string.Empty;

    private string? _downloadingFileName;

    protected override async Task OnInitializedAsync()
    {
        await LoadAllAsync();
    }

    private async Task LoadAllAsync()
    {
        _loading = true;
        try
        {
            _settings = await BackupService.GetSettingsAsync();
            _hasStoredEncryptionPassword = !string.IsNullOrEmpty(_settings.EncryptionPassword);
            _encryptionPasswordInput = string.Empty;

            _targets = await BackupService.GetTargetsAsync();
            _history = await BackupService.GetHistoryAsync();
            _status = await BackupService.GetStatusAsync();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Fehler beim Laden: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task RunBackupNow()
    {
        _runningManual = true;
        _errorMessage = null;
        try
        {
            var result = await BackupService.RunBackupAsync(isManual: true);
            Snackbar.Add(
                result.Success ? "Backup erfolgreich abgeschlossen." : $"Backup fehlgeschlagen: {result.Error}",
                result.Success ? Severity.Success : Severity.Error);

            _targets = await BackupService.GetTargetsAsync();
            _history = await BackupService.GetHistoryAsync();
            _status = await BackupService.GetStatusAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Fehler beim Backup: {ex.Message}", Severity.Error);
        }
        finally
        {
            _runningManual = false;
        }
    }

    private async Task SaveSettingsAsync()
    {
        _saving = true;
        _errorMessage = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(_encryptionPasswordInput))
            {
                _settings.EncryptionPassword = _encryptionPasswordInput;
            }

            await BackupService.UpdateSettingsAsync(_settings);
            Snackbar.Add("Backup-Einstellungen gespeichert.", Severity.Success);
            await LoadAllAsync();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Fehler beim Speichern: {ex.Message}";
            Snackbar.Add("Fehler beim Speichern der Backup-Einstellungen.", Severity.Error);
        }
        finally
        {
            _saving = false;
        }
    }

    private async Task AddTargetAsync()
    {
        var parameters = new DialogParameters<BackupTargetDialog>
        {
            { x => x.Target, new BackupTarget() }
        };

        var dialog = await DialogService.ShowAsync<BackupTargetDialog>("Backup-Ziel hinzufügen", parameters);
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: BackupTarget target })
        {
            await BackupService.AddTargetAsync(target);
            Snackbar.Add("Backup-Ziel hinzugefügt.", Severity.Success);
            _targets = await BackupService.GetTargetsAsync();
        }
    }

    private async Task EditTargetAsync(BackupTarget target)
    {
        var parameters = new DialogParameters<BackupTargetDialog>
        {
            { x => x.Target, target },
            { x => x.IsEdit, true }
        };

        var dialog = await DialogService.ShowAsync<BackupTargetDialog>("Backup-Ziel bearbeiten", parameters);
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: BackupTarget edited })
        {
            await BackupService.UpdateTargetAsync(edited);
            Snackbar.Add("Backup-Ziel gespeichert.", Severity.Success);
            _targets = await BackupService.GetTargetsAsync();
        }
    }

    private async Task DeleteTargetAsync(BackupTarget target)
    {
        var parameters = new DialogParameters<ConfirmDialog>
        {
            { x => x.ContentText, $"Backup-Ziel '{target.Name}' wirklich löschen? Bereits vorhandene Backups auf diesem Ziel bleiben dort erhalten." },
            { x => x.ConfirmButtonText, "Löschen" },
            { x => x.ConfirmButtonColor, Color.Error }
        };

        var dialog = await DialogService.ShowAsync<ConfirmDialog>("Backup-Ziel löschen", parameters);
        var result = await dialog.Result;

        if (result != null && !result.Canceled)
        {
            await BackupService.DeleteTargetAsync(target.Id);
            Snackbar.Add("Backup-Ziel gelöscht.", Severity.Success);
            _targets = await BackupService.GetTargetsAsync();
        }
    }

    private async Task TestConnectionAsync(BackupTarget target)
    {
        try
        {
            var (success, error) = await BackupService.TestTargetConnectionAsync(target.Id);
            Snackbar.Add(
                success ? $"Verbindung zu '{target.Name}' erfolgreich." : $"Verbindung zu '{target.Name}' fehlgeschlagen: {error}",
                success ? Severity.Success : Severity.Error);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Verbindungstest fehlgeschlagen: {ex.Message}", Severity.Error);
        }
    }

    private async Task OpenRestoreDialogAsync()
    {
        var parameters = new DialogParameters<RestoreDialog>
        {
            { x => x.Targets, _targets }
        };
        var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseOnEscapeKey = true };

        var dialog = await DialogService.ShowAsync<RestoreDialog>("Backup wiederherstellen", parameters, options);
        var result = await dialog.Result;

        if (result is { Canceled: false })
        {
            Snackbar.Add("Wiederherstellung abgeschlossen. Ein Neustart des Host-Containers ist erforderlich.", Severity.Success);
        }
    }

    private async Task DownloadAsync(int targetId, string fileName)
    {
        _downloadingFileName = fileName;
        try
        {
            await using var stream = await BackupService.OpenBackupFileForDownloadAsync(targetId, fileName);
            using var streamRef = new DotNetStreamReference(stream, leaveOpen: false);
            await JSRuntime.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Download fehlgeschlagen: {ex.Message}", Severity.Error);
        }
        finally
        {
            _downloadingFileName = null;
        }
    }

    private static string FormatBytes(long? bytes)
    {
        if (!bytes.HasValue)
            return "–";

        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double size = bytes.Value;
        var unitIndex = 0;

        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:0.##} {units[unitIndex]}";
    }
}
