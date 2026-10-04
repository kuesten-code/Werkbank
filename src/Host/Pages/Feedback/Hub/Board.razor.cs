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

    protected override async Task OnInitializedAsync()
    {
        if (ModeState.Role != FeedbackRole.Hub)
            return;

        _instances = await InstanceService.GetInstancesAsync();
        _modules = await BoardService.GetModulesAsync();
        _reports = await BoardService.GetReportsAsync(_filter);
    }

    private async Task ApplyFilterAsync(FeedbackBoardFilter filter)
    {
        _filter = filter;
        _reports = await BoardService.GetReportsAsync(_filter);
        _dropContainer?.Refresh();
    }

    private async Task OnItemDroppedAsync(MudItemDropInfo<FeedbackReport> dropInfo)
    {
        if (dropInfo.Item == null || !Enum.TryParse<FeedbackStatus>(dropInfo.DropzoneIdentifier, out var status)
            || dropInfo.Item.Status == status)
        {
            return;
        }

        await BoardService.SetStatusAsync(dropInfo.Item.Id, status);
        dropInfo.Item.Status = status;
        if (status != FeedbackStatus.Abgelehnt)
            dropInfo.Item.DuplicateOfId = null;

        Snackbar.Add($"#{dropInfo.Item.Id} → {FeedbackLabels.Of(status)}", Severity.Success);
    }
}
