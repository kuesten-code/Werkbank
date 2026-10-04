using Kuestencode.Shared.UI.Components;
using Kuestencode.Werkbank.Host.Models.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback.Hub;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Kuestencode.Werkbank.Host.Pages.Feedback.Hub;

public partial class Instances
{
    [Inject] private IFeedbackInstanceService InstanceService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private List<FeedbackInstance> _instances = new();
    private string _newName = string.Empty;
    private string? _shownApiKey;
    private string? _shownApiKeyFor;

    protected override async Task OnInitializedAsync()
    {
        _instances = await InstanceService.GetInstancesAsync();
    }

    private async Task CreateAsync()
    {
        var (instance, apiKey) = await InstanceService.CreateInstanceAsync(_newName);
        ShowApiKey(instance.Name, apiKey);
        _newName = string.Empty;
        _instances = await InstanceService.GetInstancesAsync();
    }

    private async Task RegenerateAsync(FeedbackInstance instance)
    {
        var parameters = new DialogParameters<ConfirmDialog>
        {
            { x => x.ContentText, $"Neuen API-Key für „{instance.Name}“ erzeugen?" },
            { x => x.WarningText, "Der bisherige Key wird sofort ungültig — die Kunden-Werkbank kann erst wieder senden, wenn der neue Key dort eingetragen ist." },
            { x => x.ConfirmButtonText, "Neu erzeugen" },
            { x => x.ConfirmButtonColor, Color.Warning }
        };

        var dialog = await DialogService.ShowAsync<ConfirmDialog>("API-Key neu erzeugen", parameters);
        var result = await dialog.Result;
        if (result is not { Canceled: false })
            return;

        ShowApiKey(instance.Name, await InstanceService.RegenerateApiKeyAsync(instance.Id));
    }

    private async Task ToggleActiveAsync(FeedbackInstance instance)
    {
        await InstanceService.SetActiveAsync(instance.Id, !instance.IsActive);
        Snackbar.Add(instance.IsActive ? $"„{instance.Name}“ deaktiviert." : $"„{instance.Name}“ aktiviert.", Severity.Success);
        _instances = await InstanceService.GetInstancesAsync();
    }

    private void ShowApiKey(string instanceName, string apiKey)
    {
        _shownApiKeyFor = instanceName;
        _shownApiKey = apiKey;
    }

    private async Task CopyApiKeyAsync()
    {
        await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", _shownApiKey);
        Snackbar.Add("API-Key kopiert.", Severity.Info);
    }
}
