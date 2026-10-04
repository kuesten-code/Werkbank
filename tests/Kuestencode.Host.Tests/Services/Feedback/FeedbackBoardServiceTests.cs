using FluentAssertions;
using Kuestencode.Shared.Contracts.Feedback;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Models.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback.Hub;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Kuestencode.Host.Tests.Services.Feedback;

public class FeedbackBoardServiceTests : IDisposable
{
    private readonly HostDbContext _context = FeedbackTestData.CreateContext();
    private readonly string _storagePath = Directory.CreateTempSubdirectory("werkbank-feedback-board-").FullName;
    private readonly FeedbackFileStore _fileStore;
    private readonly FeedbackBoardService _service;
    private readonly FeedbackInstance _instanceA = new() { Name = "Kunde A", ApiKeyHash = "a" };
    private readonly FeedbackInstance _instanceB = new() { Name = "Kunde B", ApiKeyHash = "b" };

    public FeedbackBoardServiceTests()
    {
        _fileStore = FeedbackTestData.CreateFileStore(_storagePath);
        _service = new FeedbackBoardService(_context, _fileStore);
        _context.FeedbackInstances.AddRange(_instanceA, _instanceB);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        if (Directory.Exists(_storagePath))
            Directory.Delete(_storagePath, true);
    }

    private async Task<FeedbackReport> AddReportAsync(FeedbackInstance instance, FeedbackType type = FeedbackType.Bug, string? module = "Faktura")
    {
        var report = new FeedbackReport
        {
            InstanceId = instance.Id, ClientReportId = Guid.NewGuid(), Type = type, Module = module,
            Title = "Titel", Actual = "Ist", ReporterName = "x", PageUrl = "/", AppVersion = "dev"
        };
        _context.FeedbackReports.Add(report);
        await _context.SaveChangesAsync();
        return report;
    }

    [Fact]
    public async Task GetReportsAsync_FiltertNachKundeTypUndModul()
    {
        await AddReportAsync(_instanceA, FeedbackType.Bug, "Faktura");
        await AddReportAsync(_instanceA, FeedbackType.Wunsch, null);
        await AddReportAsync(_instanceB, FeedbackType.Bug, "Rapport");

        (await _service.GetReportsAsync(new FeedbackBoardFilter())).Should().HaveCount(3);
        (await _service.GetReportsAsync(new FeedbackBoardFilter(InstanceId: _instanceA.Id))).Should().HaveCount(2);
        (await _service.GetReportsAsync(new FeedbackBoardFilter(Type: FeedbackType.Wunsch))).Should().ContainSingle();
        (await _service.GetReportsAsync(new FeedbackBoardFilter(Module: "Rapport"))).Single().Instance.Name.Should().Be("Kunde B");
        (await _service.GetModulesAsync()).Should().Equal("Faktura", "Rapport");
    }

    [Fact]
    public async Task SetStatusAsync_AendertStatusUndAktualisiertZeitstempel()
    {
        var report = await AddReportAsync(_instanceA);
        var before = report.UpdatedAt;
        await Task.Delay(10);

        await _service.SetStatusAsync(report.Id, FeedbackStatus.InArbeit);

        var stored = await _context.FeedbackReports.AsNoTracking().SingleAsync();
        stored.Status.Should().Be(FeedbackStatus.InArbeit);
        stored.UpdatedAt.Should().BeAfter(before);
    }

    [Fact]
    public async Task SetStatusAsync_AusAbgelehntHeraus_HebtDuplikatVerweisAuf()
    {
        var original = await AddReportAsync(_instanceA);
        var duplicate = await AddReportAsync(_instanceA);
        await _service.MarkAsDuplicateAsync(duplicate.Id, original.Id);

        await _service.SetStatusAsync(duplicate.Id, FeedbackStatus.Eingeordnet);

        (await _context.FeedbackReports.AsNoTracking().SingleAsync(r => r.Id == duplicate.Id)).DuplicateOfId.Should().BeNull();
    }

    [Fact]
    public async Task MarkAsDuplicateAsync_SetztVerweisUndAbgelehnt()
    {
        var original = await AddReportAsync(_instanceB);
        var duplicate = await AddReportAsync(_instanceA);

        await _service.MarkAsDuplicateAsync(duplicate.Id, original.Id);

        var stored = await _context.FeedbackReports.AsNoTracking().SingleAsync(r => r.Id == duplicate.Id);
        stored.DuplicateOfId.Should().Be(original.Id);
        stored.Status.Should().Be(FeedbackStatus.Abgelehnt);
    }

    [Fact]
    public async Task MarkAsDuplicateAsync_UngueltigeOriginale_WerdenAbgelehnt()
    {
        var a = await AddReportAsync(_instanceA);
        var b = await AddReportAsync(_instanceA);
        var c = await AddReportAsync(_instanceA);
        await _service.MarkAsDuplicateAsync(b.Id, a.Id);

        await FluentActions.Invoking(() => _service.MarkAsDuplicateAsync(a.Id, a.Id)).Should().ThrowAsync<FeedbackValidationException>();
        await FluentActions.Invoking(() => _service.MarkAsDuplicateAsync(a.Id, 999)).Should().ThrowAsync<FeedbackValidationException>();
        await FluentActions.Invoking(() => _service.MarkAsDuplicateAsync(c.Id, b.Id)).Should().ThrowAsync<FeedbackValidationException>();
    }

    [Fact]
    public async Task AddHubCommentAsync_InternerKommentar_MarkiertMeldungNichtFuersPolling()
    {
        var report = await AddReportAsync(_instanceA);
        var before = (await _context.FeedbackReports.AsNoTracking().SingleAsync()).UpdatedAt;
        await Task.Delay(10);

        await _service.AddHubCommentAsync(report.Id, "Nur für mich", isInternal: true);

        (await _context.FeedbackReports.AsNoTracking().SingleAsync()).UpdatedAt.Should().Be(before);
        var comment = await _context.FeedbackComments.SingleAsync();
        comment.IsInternal.Should().BeTrue();
        comment.Author.Should().Be(FeedbackCommentAuthor.Hub);
    }

    [Fact]
    public async Task AddHubCommentAsync_OeffentlicherKommentar_MarkiertMeldungFuersPolling()
    {
        var report = await AddReportAsync(_instanceA);
        var before = (await _context.FeedbackReports.AsNoTracking().SingleAsync()).UpdatedAt;
        await Task.Delay(10);

        await _service.AddHubCommentAsync(report.Id, "Ist behoben", isInternal: false);

        (await _context.FeedbackReports.AsNoTracking().SingleAsync()).UpdatedAt.Should().BeAfter(before);
    }

    [Fact]
    public async Task AddHubCommentAsync_LeererText_Wirft()
    {
        var report = await AddReportAsync(_instanceA);

        await FluentActions.Invoking(() => _service.AddHubCommentAsync(report.Id, "", false)).Should().ThrowAsync<FeedbackValidationException>();
    }

    [Fact]
    public async Task GetReportAsync_LiefertKommentareUndAnhaenge()
    {
        var report = await AddReportAsync(_instanceA);
        await _service.AddHubCommentAsync(report.Id, "Intern", true);
        _context.FeedbackAttachments.Add(new FeedbackAttachment
        {
            ReportId = report.Id, FileName = "a.png", ContentType = "image/png", Size = 10,
            StoragePath = await _fileStore.SaveAsync("hub/1/x", ".png", FeedbackTestData.Png)
        });
        await _context.SaveChangesAsync();

        var detail = await _service.GetReportAsync(report.Id);

        detail!.Comments.Should().ContainSingle(c => c.IsInternal);
        detail.Attachments.Should().ContainSingle();
        detail.Instance.Name.Should().Be("Kunde A");
        (await _service.GetReportAsync(999)).Should().BeNull();
    }

    [Fact]
    public async Task OpenAttachmentAsync_LiefertInhaltOderNull()
    {
        var report = await AddReportAsync(_instanceA);
        var attachment = new FeedbackAttachment
        {
            ReportId = report.Id, FileName = "a.png", ContentType = "image/png", Size = 10,
            StoragePath = await _fileStore.SaveAsync("hub/1/x", ".png", FeedbackTestData.Png)
        };
        _context.FeedbackAttachments.Add(attachment);
        await _context.SaveChangesAsync();

        var result = await _service.OpenAttachmentAsync(attachment.Id);

        using var stream = result!.Value.Content;
        stream.Length.Should().Be(FeedbackTestData.Png.Length);
        (await _service.OpenAttachmentAsync(999)).Should().BeNull();
    }

    [Fact]
    public async Task SetStatusAsync_UnbekannteMeldung_Wirft()
    {
        await FluentActions.Invoking(() => _service.SetStatusAsync(999, FeedbackStatus.Erledigt)).Should().ThrowAsync<InvalidOperationException>();
    }
}
