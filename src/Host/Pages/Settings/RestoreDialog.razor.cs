using System.Security.Claims;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Models;
using Kuestencode.Werkbank.Host.Services;
using Kuestencode.Werkbank.Host.Services.Backup;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace Kuestencode.Werkbank.Host.Pages.Settings;

public partial class RestoreDialog
{
    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public List<BackupTarget> Targets { get; set; } = new();

    [Inject] private IBackupService BackupService { get; set; } = default!;
    [Inject] private HostDbContext DbContext { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private IPasswordService PasswordService { get; set; } = default!;

    private int _targetId;
    private List<BackupFileInfo>? _files;
    private string? _fileName;
    private string _confirmPassword = string.Empty;
    private string _confirmText = string.Empty;
    private string? _errorMessage;
    private bool _loadingFiles;
    private bool _restoring;

    private void Cancel() => MudDialog.Cancel();

    private async Task OnTargetChangedAsync(int targetId)
    {
        _targetId = targetId;
        _fileName = null;
        _confirmPassword = string.Empty;
        _confirmText = string.Empty;
        _errorMessage = null;
        _files = null;

        if (targetId <= 0)
            return;

        _loadingFiles = true;
        try
        {
            _files = await BackupService.ListBackupsOnTargetAsync(targetId);
        }
        catch (Exception ex)
        {
            _errorMessage = $"Backups konnten nicht geladen werden: {ex.Message}";
            _files = new List<BackupFileInfo>();
        }
        finally
        {
            _loadingFiles = false;
        }
    }

    private async Task ExecuteRestoreAsync()
    {
        if (string.IsNullOrEmpty(_fileName))
            return;

        _restoring = true;
        _errorMessage = null;

        try
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var userId = authState.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userId, out var id))
            {
                _errorMessage = "Aktueller Benutzer konnte nicht ermittelt werden.";
                return;
            }

            var member = await DbContext.TeamMembers.FirstOrDefaultAsync(m => m.Id == id && m.IsActive);
            if (member?.PasswordHash == null || !PasswordService.VerifyPassword(member.PasswordHash, _confirmPassword))
            {
                _errorMessage = "Passwort ist falsch.";
                return;
            }

            var result = await BackupService.RestoreAsync(_targetId, _fileName);
            if (result.Success)
            {
                MudDialog.Close(DialogResult.Ok(true));
            }
            else
            {
                _errorMessage = result.Error;
            }
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
        }
        finally
        {
            _restoring = false;
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
