using Kuestencode.Shared.Contracts.Feedback;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Models.Feedback;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Host.Services.Feedback.Hub;

public class FeedbackHubApiService : IFeedbackHubApiService
{
    private readonly HostDbContext _context;
    private readonly IFeedbackSettingsService _settingsService;
    private readonly IFeedbackFileStore _fileStore;
    private readonly IFeedbackNotifier _notifier;
    private readonly ILogger<FeedbackHubApiService> _logger;

    public FeedbackHubApiService(
        HostDbContext context,
        IFeedbackSettingsService settingsService,
        IFeedbackFileStore fileStore,
        IFeedbackNotifier notifier,
        ILogger<FeedbackHubApiService> logger)
    {
        _context = context;
        _settingsService = settingsService;
        _fileStore = fileStore;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task<(FeedbackReportDto Report, bool Created)> CreateReportAsync(
        int instanceId, CreateFeedbackReportRequest request, IReadOnlyList<FeedbackUpload> uploads, CancellationToken ct = default)
    {
        var existing = await FindByClientReportIdAsync(instanceId, request.ClientReportId);
        if (existing != null)
            return (FeedbackDtoMapper.ToDto(existing), false);

        var settings = await _settingsService.GetSettingsAsync();
        var errors = FeedbackReportValidator.Validate(request);
        var (uploadErrors, files) = FeedbackUploadPolicy.Validate(uploads, settings.MaxFileSizeMb, settings.MaxReportSizeMb);
        errors.AddRange(uploadErrors);
        if (errors.Count > 0)
            throw new FeedbackValidationException(errors);

        var report = new FeedbackReport
        {
            InstanceId = instanceId,
            ClientReportId = request.ClientReportId,
            Type = request.Type,
            Module = string.IsNullOrWhiteSpace(request.Module) ? null : request.Module.Trim(),
            Title = request.Title.Trim(),
            Expected = string.IsNullOrWhiteSpace(request.Expected) ? null : request.Expected.Trim(),
            Actual = request.Actual.Trim(),
            ReporterName = Truncate(request.ReporterName, 200),
            PageUrl = Truncate(request.PageUrl, 1000),
            AppVersion = Truncate(request.AppVersion, 50),
            Status = FeedbackStatus.Neu
        };

        var folder = $"hub/{instanceId}/{request.ClientReportId:N}";
        foreach (var file in files)
        {
            report.Attachments.Add(new FeedbackAttachment
            {
                FileName = file.FileName,
                ContentType = file.ContentType,
                Size = file.Content.Length,
                StoragePath = await _fileStore.SaveAsync(folder, file.Extension, file.Content, ct)
            });
        }

        _context.FeedbackReports.Add(report);
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Zwei parallele Zustellungen derselben ClientReportId: der Unique-Index hat die
            // zweite abgewiesen — dann gilt die bereits gespeicherte Meldung.
            _context.Entry(report).State = EntityState.Detached;
            foreach (var attachment in report.Attachments)
                _fileStore.Delete(attachment.StoragePath);

            var winner = await FindByClientReportIdAsync(instanceId, request.ClientReportId);
            if (winner == null)
                throw;
            return (FeedbackDtoMapper.ToDto(winner), false);
        }

        var instanceName = await _context.FeedbackInstances
            .Where(i => i.Id == instanceId).Select(i => i.Name).FirstAsync(ct);
        await _notifier.NotifyNewReportAsync(report, instanceName);

        _logger.LogInformation("Feedback-Meldung {ReportId} von Instanz {InstanceId} angelegt", report.Id, instanceId);
        return (FeedbackDtoMapper.ToDto(report), true);
    }

    public async Task<FeedbackSyncResponse> GetChangesAsync(int instanceId, DateTime? since)
    {
        var serverTime = DateTime.UtcNow;

        var query = _context.FeedbackReports.AsNoTracking()
            .Include(r => r.Comments)
            .Where(r => r.InstanceId == instanceId);

        if (since.HasValue)
        {
            var sinceUtc = DateTime.SpecifyKind(since.Value.ToUniversalTime(), DateTimeKind.Utc);
            query = query.Where(r => r.UpdatedAt > sinceUtc);
        }

        var reports = await query.OrderBy(r => r.Id).ToListAsync();
        return new FeedbackSyncResponse
        {
            ServerTime = serverTime,
            Reports = reports.Select(FeedbackDtoMapper.ToDto).ToList()
        };
    }

    public async Task<FeedbackCommentDto?> AddCustomerCommentAsync(int instanceId, int reportId, CreateFeedbackCommentRequest request)
    {
        var report = await _context.FeedbackReports
            .FirstOrDefaultAsync(r => r.Id == reportId && r.InstanceId == instanceId);
        if (report == null)
            return null;

        var errors = FeedbackReportValidator.ValidateComment(request.Text);
        if (errors.Count > 0)
            throw new FeedbackValidationException(errors);

        if (request.ClientCommentId != Guid.Empty)
        {
            var existing = await _context.FeedbackComments.AsNoTracking()
                .FirstOrDefaultAsync(c => c.ReportId == reportId && c.ClientCommentId == request.ClientCommentId);
            if (existing != null)
                return FeedbackDtoMapper.ToDto(existing);
        }

        var comment = new FeedbackComment
        {
            ReportId = reportId,
            ClientCommentId = request.ClientCommentId == Guid.Empty ? null : request.ClientCommentId,
            Author = FeedbackCommentAuthor.Kunde,
            Text = request.Text.Trim(),
            IsInternal = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.FeedbackComments.Add(comment);
        report.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return FeedbackDtoMapper.ToDto(comment);
    }

    private Task<FeedbackReport?> FindByClientReportIdAsync(int instanceId, Guid clientReportId) =>
        _context.FeedbackReports.AsNoTracking()
            .Include(r => r.Comments)
            .FirstOrDefaultAsync(r => r.InstanceId == instanceId && r.ClientReportId == clientReportId);

    private static string Truncate(string? value, int maxLength)
    {
        var trimmed = value?.Trim() ?? "";
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
