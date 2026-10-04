using Kuestencode.Shared.Contracts.Feedback;
using Kuestencode.Werkbank.Host.Models.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback.Hub;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Kuestencode.Werkbank.Host.Pages.Feedback.Hub;

public partial class ReportDetail
{
    [Inject] private IFeedbackBoardService BoardService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    [Parameter] public int ReportId { get; set; }

    private FeedbackReport? _report;
    private bool _loading = true;
    private string _commentText = string.Empty;
    private bool _commentInternal;
    private int? _duplicateOfId;

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        await ReloadAsync();
        _loading = false;
    }

    private async Task ReloadAsync()
    {
        _report = await BoardService.GetReportAsync(ReportId);
    }

    private async Task SetStatusAsync(FeedbackStatus status)
    {
        await BoardService.SetStatusAsync(ReportId, status);
        await ReloadAsync();
        Snackbar.Add($"Status: {FeedbackLabels.Of(status)}", Severity.Success);
    }

    private async Task AddCommentAsync()
    {
        try
        {
            await BoardService.AddHubCommentAsync(ReportId, _commentText, _commentInternal);
            _commentText = string.Empty;
            await ReloadAsync();
        }
        catch (FeedbackValidationException ex)
        {
            Snackbar.Add(string.Join(" ", ex.Errors), Severity.Error);
        }
    }

    private async Task MarkAsDuplicateAsync()
    {
        try
        {
            await BoardService.MarkAsDuplicateAsync(ReportId, _duplicateOfId!.Value);
            _duplicateOfId = null;
            await ReloadAsync();
            Snackbar.Add("Als Duplikat markiert.", Severity.Success);
        }
        catch (FeedbackValidationException ex)
        {
            Snackbar.Add(string.Join(" ", ex.Errors), Severity.Error);
        }
    }
}
