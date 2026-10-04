using Kuestencode.Shared.Contracts.Feedback;
using Kuestencode.Werkbank.Host.Models.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback.Hub;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Kuestencode.Werkbank.Host.Pages.Feedback.Hub;

public partial class Board
{
    [Inject] private IFeedbackBoardService BoardService { get; set; } = default!;
    [Inject] private IFeedbackInstanceService InstanceService { get; set; } = default!;
    [Inject] private IFeedbackModeState ModeState { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private MudDropContainer<FeedbackReport>? _dropContainer;
    private List<FeedbackReport> _reports = new();
    private List<FeedbackInstance> _instances = new();
    private List<string> _modules = new();
    private FeedbackBoardFilter _filter = new();
    private bool _loaded;
    private int _containerVersion;

    protected override async Task OnInitializedAsync()
    {
        if (ModeState.Role != FeedbackRole.Hub)
            return;

        _instances = await InstanceService.GetInstancesAsync();
        _modules = await BoardService.GetModulesAsync();
        _reports = await BoardService.GetReportsAsync(_filter);
        _loaded = true;
    }

    private async Task ApplyFilterAsync(FeedbackBoardFilter filter)
    {
        _filter = filter;
        _reports = await BoardService.GetReportsAsync(_filter);
        _containerVersion++;
    }

    // Optimistisch: Karte sofort in die Zielspalte setzen, bei einem Fehler zurück.
    private async Task OnItemDroppedAsync(MudItemDropInfo<FeedbackReport> dropInfo)
    {
        var report = dropInfo.Item;
        if (report == null || !Enum.TryParse<FeedbackStatus>(dropInfo.DropzoneIdentifier, out var status) || report.Status == status)
            return;

        var previousStatus = report.Status;
        var previousDuplicateOfId = report.DuplicateOfId;
        report.Status = status;
        if (status != FeedbackStatus.Abgelehnt)
            report.DuplicateOfId = null;

        try
        {
            await BoardService.SetStatusAsync(report.Id, status);
        }
        catch (Exception ex)
        {
            report.Status = previousStatus;
            report.DuplicateOfId = previousDuplicateOfId;
            _dropContainer?.Refresh();
            Snackbar.Add($"#{report.Id} konnte nicht verschoben werden: {ex.Message}", Severity.Error);
        }
    }
}
