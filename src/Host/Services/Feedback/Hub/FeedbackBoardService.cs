using Kuestencode.Shared.Contracts.Feedback;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Models.Feedback;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Host.Services.Feedback.Hub;

public class FeedbackBoardService : IFeedbackBoardService
{
    private readonly HostDbContext _context;
    private readonly IFeedbackFileStore _fileStore;

    public FeedbackBoardService(HostDbContext context, IFeedbackFileStore fileStore)
    {
        _context = context;
        _fileStore = fileStore;
    }

    public Task<List<FeedbackReport>> GetReportsAsync(FeedbackBoardFilter filter)
    {
        var query = _context.FeedbackReports.AsNoTracking().Include(r => r.Instance).AsQueryable();

        if (filter.InstanceId.HasValue)
            query = query.Where(r => r.InstanceId == filter.InstanceId);
        if (filter.Type.HasValue)
            query = query.Where(r => r.Type == filter.Type);
        if (!string.IsNullOrEmpty(filter.Module))
            query = query.Where(r => r.Module == filter.Module);

        return query.OrderByDescending(r => r.UpdatedAt).ToListAsync();
    }

    public async Task<List<string>> GetModulesAsync()
    {
        var modules = await _context.FeedbackReports.AsNoTracking()
            .Where(r => r.Module != null)
            .Select(r => r.Module!)
            .Distinct()
            .ToListAsync();
        return modules.Order().ToList();
    }

    public Task<FeedbackReport?> GetReportAsync(int reportId) =>
        _context.FeedbackReports.AsNoTracking()
            .Include(r => r.Instance)
            .Include(r => r.Comments)
            .Include(r => r.Attachments)
            .FirstOrDefaultAsync(r => r.Id == reportId);

    public async Task SetStatusAsync(int reportId, FeedbackStatus status)
    {
        var report = await FindTrackedAsync(reportId);
        report.Status = status;

        // Wer eine Duplikat-Karte wieder aus "Abgelehnt" holt, hebt damit auch den Verweis auf.
        if (status != FeedbackStatus.Abgelehnt)
            report.DuplicateOfId = null;

        await _context.SaveChangesAsync();
    }

    public async Task AddHubCommentAsync(int reportId, string text, bool isInternal)
    {
        var errors = FeedbackReportValidator.ValidateComment(text);
        if (errors.Count > 0)
            throw new FeedbackValidationException(errors);

        var report = await FindTrackedAsync(reportId);
        _context.FeedbackComments.Add(new FeedbackComment
        {
            ReportId = reportId,
            Author = FeedbackCommentAuthor.Hub,
            Text = text.Trim(),
            IsInternal = isInternal,
            CreatedAt = DateTime.UtcNow
        });

        // Nur sichtbare Änderungen markieren die Meldung für das Polling der Kunden-Instanz.
        if (!isInternal)
            report.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public Task<List<FeedbackReport>> GetDuplicateCandidatesAsync(int reportId) =>
        _context.FeedbackReports.AsNoTracking()
            .Include(r => r.Instance)
            .Where(r => r.Id != reportId && r.DuplicateOfId == null && r.HubDuplicateOfId == null)
            .OrderByDescending(r => r.Id)
            .ToListAsync();

    public Task<List<FeedbackReport>> GetHubDuplicatesOfAsync(int reportId) =>
        _context.FeedbackReports.AsNoTracking()
            .Include(r => r.Instance)
            .Where(r => r.HubDuplicateOfId == reportId)
            .OrderBy(r => r.Id)
            .ToListAsync();

    public async Task MarkAsDuplicateAsync(int reportId, int originalReportId)
    {
        var (report, original) = await LoadDuplicatePairAsync(reportId, originalReportId);

        if (original.InstanceId != report.InstanceId)
            throw new FeedbackValidationException(new[] { $"Meldung #{originalReportId} gehört zu einem anderen Kunden — bitte als Hub-Duplikat verknüpfen." });
        if (original.DuplicateOfId.HasValue)
            throw new FeedbackValidationException(new[] { $"Meldung #{originalReportId} ist selbst ein Duplikat von #{original.DuplicateOfId} — bitte auf das Original verweisen." });

        report.DuplicateOfId = originalReportId;
        report.Status = FeedbackStatus.Abgelehnt;
        await _context.SaveChangesAsync();
    }

    public async Task MarkAsHubDuplicateAsync(int reportId, int originalReportId)
    {
        var (report, original) = await LoadDuplicatePairAsync(reportId, originalReportId);

        if (original.InstanceId == report.InstanceId)
            throw new FeedbackValidationException(new[] { $"Meldung #{originalReportId} gehört zum selben Kunden — bitte als normales Duplikat markieren." });
        if (original.HubDuplicateOfId.HasValue)
            throw new FeedbackValidationException(new[] { $"Meldung #{originalReportId} ist selbst ein Hub-Duplikat von #{original.HubDuplicateOfId} — bitte auf das Original verweisen." });

        report.HubDuplicateOfId = originalReportId;
        await _context.SaveChangesAsync();
    }

    public async Task RemoveHubDuplicateAsync(int reportId)
    {
        var report = await FindTrackedAsync(reportId);
        report.HubDuplicateOfId = null;
        await _context.SaveChangesAsync();
    }

    private async Task<(FeedbackReport Report, FeedbackReport Original)> LoadDuplicatePairAsync(int reportId, int originalReportId)
    {
        if (reportId == originalReportId)
            throw new FeedbackValidationException(new[] { "Eine Meldung kann kein Duplikat von sich selbst sein." });

        var original = await _context.FeedbackReports.AsNoTracking().FirstOrDefaultAsync(r => r.Id == originalReportId)
            ?? throw new FeedbackValidationException(new[] { $"Meldung #{originalReportId} existiert nicht." });

        return (await FindTrackedAsync(reportId), original);
    }

    public async Task<(FeedbackAttachment Attachment, Stream Content)?> OpenAttachmentAsync(int attachmentId)
    {
        var attachment = await _context.FeedbackAttachments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == attachmentId);
        return attachment == null ? null : (attachment, _fileStore.OpenRead(attachment.StoragePath));
    }

    private async Task<FeedbackReport> FindTrackedAsync(int reportId) =>
        await _context.FeedbackReports.FindAsync(reportId)
            ?? throw new InvalidOperationException($"Meldung {reportId} nicht gefunden.");
}
