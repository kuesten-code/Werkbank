using Kuestencode.Werkbank.Host.Models.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback.Client;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Kuestencode.Werkbank.Host.Pages.Feedback;

public partial class MyReports
{
    [Inject] private IFeedbackClientService ClientService { get; set; } = default!;
    [Inject] private IFeedbackSyncService SyncService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private List<FeedbackClientReport> _reports = new();
    private readonly Dictionary<int, string> _replyTexts = new();
    private bool _loading = true;
    private bool _syncing;
    private string? _syncError;

    protected override async Task OnInitializedAsync()
    {
        _reports = await ClientService.GetReportsAsync();
        _loading = false;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Zuerst den lokalen Cache zeigen, dann beim Hub nachfragen — die Liste ist damit auch
        // offline sofort nutzbar.
        if (firstRender)
        {
            await SyncAsync();
            StateHasChanged();
        }
    }

    private async Task SyncAsync()
    {
        if (_syncing)
            return;

        _syncing = true;
        try
        {
            var result = await SyncService.SyncAsync(poll: true);
            _syncError = result.Error;
            _reports = await ClientService.GetReportsAsync();
        }
        finally
        {
            _syncing = false;
        }
    }

    private string GetReplyText(int reportId) => _replyTexts.GetValueOrDefault(reportId, "");

    private async Task ReplyAsync(int reportId)
    {
        try
        {
            // Die Antwort steht sofort im Verlauf ("Wird gesendet …"); das Senden übernimmt
            // die Outbox im Hintergrund, die Seite wartet nicht auf den Hub.
            await ClientService.AddReplyAsync(reportId, GetReplyText(reportId));
            _replyTexts.Remove(reportId);
            _reports = await ClientService.GetReportsAsync();
        }
        catch (FeedbackValidationException ex)
        {
            Snackbar.Add(string.Join(" ", ex.Errors), Severity.Error);
        }
    }
}
