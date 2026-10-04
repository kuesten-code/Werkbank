using Kuestencode.Shared.Contracts.Feedback;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Models.Feedback;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Host.Services.Feedback.Client;

public class FeedbackClientService : IFeedbackClientService
{
    private readonly HostDbContext _context;
    private readonly IFeedbackFileStore _fileStore;
    private readonly IFeedbackOutboxSignal _outboxSignal;
    private readonly IFeedbackSettingsService _settingsService;
    private readonly IFeedbackHubClient _hubClient;

    public FeedbackClientService(
        HostDbContext context,
        IFeedbackFileStore fileStore,
        IFeedbackOutboxSignal outboxSignal,
        IFeedbackSettingsService settingsService,
        IFeedbackHubClient hubClient)
    {
        _context = context;
        _fileStore = fileStore;
        _outboxSignal = outboxSignal;
        _settingsService = settingsService;
        _hubClient = hubClient;
    }

    public async Task<FeedbackClientReport> CreateReportAsync(CreateFeedbackReportRequest request, IReadOnlyList<FeedbackUpload> files)
    {
        request.ClientReportId = Guid.NewGuid();

        var errors = FeedbackReportValidator.Validate(request);
        var (fileErrors, validFiles) = FeedbackUploadPolicy.Validate(
            files, FeedbackUploadPolicy.DefaultMaxFileSizeMb, FeedbackUploadPolicy.DefaultMaxReportSizeMb);
        errors.AddRange(fileErrors);
        if (errors.Count > 0)
            throw new FeedbackValidationException(errors);

        var report = new FeedbackClientReport
        {
            ClientReportId = request.ClientReportId,
            Type = request.Type,
            Module = string.IsNullOrWhiteSpace(request.Module) ? null : request.Module.Trim(),
            Title = request.Title.Trim(),
            Expected = string.IsNullOrWhiteSpace(request.Expected) ? null : request.Expected.Trim(),
            Actual = request.Actual.Trim(),
            ReporterName = request.ReporterName,
            PageUrl = request.PageUrl,
            AppVersion = request.AppVersion,
            SyncState = FeedbackSyncState.Ausstehend
        };

        var folder = $"outbox/{report.ClientReportId:N}";
        foreach (var file in validFiles)
        {
            report.Attachments.Add(new FeedbackClientAttachment
            {
                FileName = file.FileName,
                ContentType = file.ContentType,
                Size = file.Content.Length,
                StoragePath = await _fileStore.SaveAsync(folder, file.Extension, file.Content)
            });
        }

        _context.FeedbackClientReports.Add(report);
        await _context.SaveChangesAsync();
        _context.Entry(report).State = EntityState.Detached;

        _outboxSignal.Signal();
        return report;
    }

    public Task<List<FeedbackClientReport>> GetReportsAsync() =>
        _context.FeedbackClientReports.AsNoTracking()
            .Include(r => r.Comments)
            .Include(r => r.Attachments)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

    public async Task AddReplyAsync(int localReportId, string text)
    {
        var errors = FeedbackReportValidator.ValidateComment(text);
        if (errors.Count > 0)
            throw new FeedbackValidationException(errors);

        if (!await _context.FeedbackClientReports.AnyAsync(r => r.Id == localReportId))
            throw new InvalidOperationException($"Meldung {localReportId} nicht gefunden.");

        _context.FeedbackClientComments.Add(new FeedbackClientComment
        {
            ClientReportId = localReportId,
            ClientCommentId = Guid.NewGuid(),
            Author = FeedbackCommentAuthor.Kunde,
            Text = text.Trim(),
            CreatedAt = DateTime.UtcNow,
            SyncState = FeedbackSyncState.Ausstehend
        });
        await _context.SaveChangesAsync();

        _outboxSignal.Signal();
    }

    public async Task<(bool Success, string Message)> TestConnectionAsync(string? hubUrl, string? apiKey)
    {
        var key = string.IsNullOrWhiteSpace(apiKey) ? await _settingsService.GetApiKeyAsync() : apiKey.Trim();
        if (string.IsNullOrWhiteSpace(hubUrl) || string.IsNullOrWhiteSpace(key))
            return (false, "Hub-URL und API-Key sind erforderlich.");

        try
        {
            var instance = await _hubClient.GetInstanceAsync(new FeedbackHubConnection(hubUrl.Trim(), key));
            return (true, $"Verbindung erfolgreich — angemeldet als „{instance.Name}“.");
        }
        catch (FeedbackHubException ex)
        {
            return (false, ex.Message);
        }
    }
}
