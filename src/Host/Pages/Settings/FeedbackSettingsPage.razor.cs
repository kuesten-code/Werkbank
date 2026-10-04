using Kuestencode.Werkbank.Host.Models.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback.Client;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Kuestencode.Werkbank.Host.Pages.Settings;

public partial class FeedbackSettingsPage
{
    [Inject] private IFeedbackSettingsService SettingsService { get; set; } = default!;
    [Inject] private IFeedbackClientService ClientService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private FeedbackSettings _settings = new();
    private string _apiKeyInput = string.Empty;
    private bool _hasStoredApiKey;
    private bool _loading = true;
    private bool _saving;
    private bool _testing;
    private (bool Success, string Message)? _testResult;
    private string? _errorMessage;

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
        await ShowConnectionStatusAsync();
    }

    // Zeigt beim Öffnen direkt, als welche Kunden-Instanz diese Werkbank am Hub angemeldet ist.
    private async Task ShowConnectionStatusAsync()
    {
        if (_settings.Role != FeedbackRole.Client || string.IsNullOrWhiteSpace(_settings.HubUrl) || !_hasStoredApiKey)
            return;

        StateHasChanged();
        await TestConnectionAsync();
    }

    private async Task LoadAsync()
    {
        _loading = true;
        try
        {
            _settings = await SettingsService.GetSettingsAsync();
            _hasStoredApiKey = !string.IsNullOrEmpty(_settings.ApiKey);
            _apiKeyInput = string.Empty;
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

    private async Task TestConnectionAsync()
    {
        if (_testing)
            return;

        _testing = true;
        _testResult = null;
        try
        {
            _testResult = await ClientService.TestConnectionAsync(_settings.HubUrl, _apiKeyInput);
        }
        finally
        {
            _testing = false;
        }
    }

    private async Task SaveAsync()
    {
        if (_saving)
            return;

        _saving = true;
        _errorMessage = null;
        try
        {
            await SettingsService.UpdateSettingsAsync(_settings, _apiKeyInput);
            Snackbar.Add("Feedback-Einstellungen gespeichert. Die Navigation aktualisiert sich beim nächsten Seitenaufruf.", Severity.Success);
            await LoadAsync();
            _testResult = null;
            await ShowConnectionStatusAsync();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Fehler beim Speichern: {ex.Message}";
        }
        finally
        {
            _saving = false;
        }
    }
}
