using Kuestencode.Shared.Contracts.Feedback;
using Kuestencode.Werkbank.Host.Models.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback.Hub;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Kuestencode.Werkbank.Host.Pages.Feedback.Hub;

/// <summary>
/// Änderungen werden optimistisch sofort angezeigt und bei einem Fehler zurückgenommen.
/// </summary>
public partial class ReportDetail
{
    [Inject] private IFeedbackBoardService BoardService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    [Parameter] public int ReportId { get; set; }

    private FeedbackReport? _report;
    private List<FeedbackReport> _candidates = new();
    private List<FeedbackReport> _hubDuplicates = new();
    private bool _loading = true;
    private string _commentText = string.Empty;
    private bool _commentInternal;
    private FeedbackReport? _duplicateOriginal;
    private FeedbackReport? _hubDuplicateOriginal;

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        _report = await BoardService.GetReportAsync(ReportId);
        if (_report != null)
        {
            _candidates = await BoardService.GetDuplicateCandidatesAsync(ReportId);
            _hubDuplicates = await BoardService.GetHubDuplicatesOfAsync(ReportId);
        }
        _loading = false;
    }

    private static string DescribeReport(FeedbackReport? report) =>
        report == null ? string.Empty : $"#{report.Id} – {report.Title}";

    private static string DescribeReportWithCustomer(FeedbackReport? report) =>
        report == null ? string.Empty : $"#{report.Id} – {report.Title} ({report.Instance.Name})";

    private Task<IEnumerable<FeedbackReport>> SearchSameCustomerAsync(string? text, CancellationToken ct) =>
        Task.FromResult(Search(_candidates.Where(r => r.InstanceId == _report!.InstanceId), text));

    private Task<IEnumerable<FeedbackReport>> SearchOtherCustomersAsync(string? text, CancellationToken ct) =>
        Task.FromResult(Search(_candidates.Where(r => r.InstanceId != _report!.InstanceId), text));

    private static IEnumerable<FeedbackReport> Search(IEnumerable<FeedbackReport> reports, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return reports;

        var term = text.Trim().TrimStart('#');
        return reports.Where(r => r.Id.ToString().StartsWith(term)
            || r.Title.Contains(term, StringComparison.OrdinalIgnoreCase)
            || r.Instance.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private async Task SetStatusAsync(FeedbackStatus status)
    {
        var report = _report!;
        var previousStatus = report.Status;
        var previousDuplicateOfId = report.DuplicateOfId;
        report.Status = status;
        if (status != FeedbackStatus.Abgelehnt)
            report.DuplicateOfId = null;

        await RunOptimisticAsync(
            () => BoardService.SetStatusAsync(ReportId, status),
            () =>
            {
                report.Status = previousStatus;
                report.DuplicateOfId = previousDuplicateOfId;
            });
    }

    private async Task AddCommentAsync()
    {
        var errors = FeedbackReportValidator.ValidateComment(_commentText);
        if (errors.Count > 0)
        {
            Snackbar.Add(string.Join(" ", errors), Severity.Warning);
            return;
        }

        var comment = new FeedbackComment
        {
            ReportId = ReportId,
            Author = FeedbackCommentAuthor.Hub,
            Text = _commentText.Trim(),
            IsInternal = _commentInternal,
            CreatedAt = DateTime.UtcNow
        };
        var text = _commentText;
        _report!.Comments.Add(comment);
        _commentText = string.Empty;

        await RunOptimisticAsync(
            () => BoardService.AddHubCommentAsync(ReportId, comment.Text, comment.IsInternal),
            () =>
            {
                _report.Comments.Remove(comment);
                _commentText = text;
            });
    }

    private async Task MarkAsDuplicateAsync()
    {
        if (_duplicateOriginal == null)
        {
            Snackbar.Add("Bitte die Original-Meldung auswählen.", Severity.Warning);
            return;
        }

        var report = _report!;
        var original = _duplicateOriginal;
        var previousStatus = report.Status;
        report.DuplicateOfId = original.Id;
        report.Status = FeedbackStatus.Abgelehnt;
        _duplicateOriginal = null;

        await RunOptimisticAsync(
            () => BoardService.MarkAsDuplicateAsync(ReportId, original.Id),
            () =>
            {
                report.DuplicateOfId = null;
                report.Status = previousStatus;
                _duplicateOriginal = original;
            });
    }

    private async Task MarkAsHubDuplicateAsync()
    {
        if (_hubDuplicateOriginal == null)
        {
            Snackbar.Add("Bitte die Meldung des anderen Kunden auswählen.", Severity.Warning);
            return;
        }

        var report = _report!;
        var original = _hubDuplicateOriginal;
        report.HubDuplicateOfId = original.Id;
        _hubDuplicateOriginal = null;

        await RunOptimisticAsync(
            () => BoardService.MarkAsHubDuplicateAsync(ReportId, original.Id),
            () =>
            {
                report.HubDuplicateOfId = null;
                _hubDuplicateOriginal = original;
            });
    }

    private async Task RemoveHubDuplicateAsync()
    {
        var report = _report!;
        var previous = report.HubDuplicateOfId;
        report.HubDuplicateOfId = null;

        await RunOptimisticAsync(
            () => BoardService.RemoveHubDuplicateAsync(ReportId),
            () => report.HubDuplicateOfId = previous);
    }

    private async Task RunOptimisticAsync(Func<Task> save, Action revert)
    {
        try
        {
            await save();
        }
        catch (FeedbackValidationException ex)
        {
            revert();
            Snackbar.Add(string.Join(" ", ex.Errors), Severity.Error);
        }
        catch (Exception ex)
        {
            revert();
            Snackbar.Add($"Speichern fehlgeschlagen: {ex.Message}", Severity.Error);
        }
    }
}
