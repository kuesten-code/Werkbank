using Kuestencode.Shared.Contracts.Feedback;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Models.Feedback;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Host.Services.Feedback.Client;

public class FeedbackSyncService : IFeedbackSyncService
{
    // Überlappung beim Polling: Änderungen, die der Hub kurz vor "ServerTime" gespeichert,
    // aber erst danach committet hat, würden sonst verloren gehen. Das Zusammenführen ist
    // idempotent, doppelt gelieferte Meldungen schaden nicht.
    private static readonly TimeSpan PollOverlap = TimeSpan.FromMinutes(1);

    // Statisch, weil der Service scoped ist: Hintergrunddienst und "Meine Meldungen" dürfen
    // nicht gleichzeitig dieselben Outbox-Einträge senden.
    private static readonly SemaphoreSlim Gate = new(1, 1);

    private readonly HostDbContext _context;
    private readonly IFeedbackSettingsService _settingsService;
    private readonly IFeedbackModeState _modeState;
    private readonly IFeedbackHubClient _hubClient;
    private readonly IFeedbackFileStore _fileStore;
    private readonly ILogger<FeedbackSyncService> _logger;

    public FeedbackSyncService(
        HostDbContext context,
        IFeedbackSettingsService settingsService,
        IFeedbackModeState modeState,
        IFeedbackHubClient hubClient,
        IFeedbackFileStore fileStore,
        ILogger<FeedbackSyncService> logger)
    {
        _context = context;
        _settingsService = settingsService;
        _modeState = modeState;
        _hubClient = hubClient;
        _fileStore = fileStore;
        _logger = logger;
    }

    public async Task<FeedbackSyncResult> SyncAsync(bool poll, CancellationToken ct = default)
    {
        if (_modeState.Role != FeedbackRole.Client)
            return new FeedbackSyncResult(false, null);

        var settings = await _settingsService.GetSettingsAsync();
        var apiKey = await _settingsService.GetApiKeyAsync();
        if (string.IsNullOrWhiteSpace(settings.HubUrl) || string.IsNullOrWhiteSpace(apiKey))
            return new FeedbackSyncResult(false, "Hub-URL oder API-Key ist nicht konfiguriert.");

        var connection = new FeedbackHubConnection(settings.HubUrl, apiKey);

        await Gate.WaitAsync(ct);
        try
        {
            await SendPendingReportsAsync(connection, ct);
            await SendPendingCommentsAsync(connection, ct);

            if (poll)
                await PollAsync(connection, settings.LastSyncAt, ct);

            return new FeedbackSyncResult(true, null);
        }
        catch (FeedbackHubException ex)
        {
            _logger.LogWarning("Feedback-Abgleich mit dem Hub fehlgeschlagen: {Message}", ex.Message);
            return new FeedbackSyncResult(true, ex.Message);
        }
        finally
        {
            Gate.Release();
        }
    }

    private async Task SendPendingReportsAsync(FeedbackHubConnection connection, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var pending = await _context.FeedbackClientReports
            .Include(r => r.Attachments)
            .Include(r => r.Comments)
            .Where(r => r.SyncState == FeedbackSyncState.Ausstehend && (r.NextAttemptAt == null || r.NextAttemptAt <= now))
            .OrderBy(r => r.Id)
            .ToListAsync(ct);

        foreach (var report in pending)
        {
            try
            {
                var files = new List<(string FileName, string ContentType, byte[] Content)>();
                foreach (var attachment in report.Attachments)
                    files.Add((attachment.FileName, attachment.ContentType, await _fileStore.ReadAllBytesAsync(attachment.StoragePath, ct)));

                var dto = await _hubClient.CreateReportAsync(connection, ToRequest(report), files, ct);

                MarkSent(report);
                ApplyHubState(report, dto);
            }
            catch (FeedbackHubException ex)
            {
                RecordFailure(report, ex);
            }

            await _context.SaveChangesAsync(ct);
        }
    }

    private async Task SendPendingCommentsAsync(FeedbackHubConnection connection, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var pending = await _context.FeedbackClientComments
            .Join(_context.FeedbackClientReports, c => c.ClientReportId, r => r.Id, (c, r) => new { Comment = c, r.HubReportId })
            .Where(x => x.HubReportId != null
                && x.Comment.SyncState == FeedbackSyncState.Ausstehend
                && (x.Comment.NextAttemptAt == null || x.Comment.NextAttemptAt <= now))
            .OrderBy(x => x.Comment.Id)
            .ToListAsync(ct);

        foreach (var item in pending)
        {
            var comment = item.Comment;
            try
            {
                var dto = await _hubClient.AddCommentAsync(connection, item.HubReportId!.Value,
                    new CreateFeedbackCommentRequest { ClientCommentId = comment.ClientCommentId ?? Guid.Empty, Text = comment.Text }, ct);

                comment.HubCommentId = dto.Id;
                MarkSent(comment);
            }
            catch (FeedbackHubException ex)
            {
                RecordFailure(comment, ex);
            }

            await _context.SaveChangesAsync(ct);
        }
    }

    private async Task PollAsync(FeedbackHubConnection connection, DateTime? lastSyncAt, CancellationToken ct)
    {
        var response = await _hubClient.GetChangesAsync(connection, lastSyncAt - PollOverlap, ct);
        if (response.Reports.Count > 0)
        {
            var clientReportIds = response.Reports.Select(r => r.ClientReportId).ToList();
            var locals = await _context.FeedbackClientReports
                .Include(r => r.Comments)
                .Where(r => clientReportIds.Contains(r.ClientReportId))
                .ToDictionaryAsync(r => r.ClientReportId, ct);

            foreach (var dto in response.Reports)
            {
                if (locals.TryGetValue(dto.ClientReportId, out var local))
                    ApplyHubState(local, dto);
            }

            await _context.SaveChangesAsync(ct);
        }

        await _settingsService.SetLastSyncAtAsync(response.ServerTime);
    }

    /// <summary>Spiegelt Status, Duplikat-Verweis und öffentliche Kommentare des Hubs in den lokalen Cache.</summary>
    private static void ApplyHubState(FeedbackClientReport local, FeedbackReportDto dto)
    {
        local.HubReportId = dto.Id;
        local.Status = dto.Status;
        local.DuplicateOfHubId = dto.DuplicateOfId;

        foreach (var hubComment in dto.Comments)
        {
            if (local.Comments.Any(c => c.HubCommentId == hubComment.Id))
                continue;

            var ownComment = hubComment.ClientCommentId.HasValue
                ? local.Comments.FirstOrDefault(c => c.ClientCommentId == hubComment.ClientCommentId)
                : null;

            if (ownComment != null)
            {
                ownComment.HubCommentId = hubComment.Id;
                MarkSent(ownComment);
                continue;
            }

            local.Comments.Add(new FeedbackClientComment
            {
                ClientCommentId = hubComment.ClientCommentId,
                HubCommentId = hubComment.Id,
                Author = hubComment.Author,
                Text = hubComment.Text,
                CreatedAt = hubComment.CreatedAt,
                SyncState = FeedbackSyncState.Gesendet
            });
        }
    }

    private static void MarkSent(IFeedbackOutboxItem item)
    {
        item.SyncState = FeedbackSyncState.Gesendet;
        item.LastError = null;
        item.NextAttemptAt = null;
    }

    private static void RecordFailure(IFeedbackOutboxItem item, FeedbackHubException ex)
    {
        item.SendAttempts++;
        item.LastError = Truncate(ex.Message);
        if (ex.IsPermanent)
            item.SyncState = FeedbackSyncState.Fehlgeschlagen;
        else
            item.NextAttemptAt = DateTime.UtcNow + FeedbackBackoff.DelayAfter(item.SendAttempts);
    }

    private static CreateFeedbackReportRequest ToRequest(FeedbackClientReport report) => new()
    {
        ClientReportId = report.ClientReportId,
        Type = report.Type,
        Module = report.Module,
        Title = report.Title,
        Expected = report.Expected,
        Actual = report.Actual,
        ReporterName = report.ReporterName,
        PageUrl = report.PageUrl,
        AppVersion = report.AppVersion
    };

    private static string Truncate(string message) => message.Length > 1000 ? message[..1000] : message;
}
