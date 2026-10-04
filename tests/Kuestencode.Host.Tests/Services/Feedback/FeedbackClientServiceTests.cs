using FluentAssertions;
using Kuestencode.Shared.Contracts.Feedback;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Models.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback.Client;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Kuestencode.Host.Tests.Services.Feedback;

public class FeedbackClientServiceTests : IDisposable
{
    private readonly HostDbContext _context = FeedbackTestData.CreateContext();
    private readonly string _storagePath = Directory.CreateTempSubdirectory("werkbank-feedback-client-").FullName;
    private readonly Mock<IFeedbackOutboxSignal> _signal = new();
    private readonly Mock<IFeedbackSettingsService> _settingsService = new();
    private readonly Mock<IFeedbackHubClient> _hubClient = new();
    private readonly FeedbackClientService _service;

    public FeedbackClientServiceTests()
    {
        _service = new FeedbackClientService(
            _context, FeedbackTestData.CreateFileStore(_storagePath), _signal.Object, _settingsService.Object, _hubClient.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_storagePath))
            Directory.Delete(_storagePath, true);
    }

    [Fact]
    public async Task CreateReportAsync_SpeichertInOutboxMitNeuerClientReportIdUndWecktSync()
    {
        var request = FeedbackTestData.Bug(Guid.Empty);
        var files = new List<FeedbackUpload> { new("bild.png", FeedbackTestData.Png), new("doku.pdf", FeedbackTestData.Pdf) };

        var report = await _service.CreateReportAsync(request, files);

        report.ClientReportId.Should().NotBeEmpty();
        var stored = await _context.FeedbackClientReports.Include(r => r.Attachments).SingleAsync();
        stored.SyncState.Should().Be(FeedbackSyncState.Ausstehend);
        stored.Attachments.Select(a => a.ContentType).Should().BeEquivalentTo("image/png", "application/pdf");
        Directory.GetFiles(_storagePath, "*", SearchOption.AllDirectories).Should().HaveCount(2);
        _signal.Verify(s => s.Signal(), Times.Once);
    }

    [Fact]
    public async Task CreateReportAsync_WunschOhneModul_SpeichertModulAlsNull()
    {
        var request = FeedbackTestData.Wish();
        request.Module = "  ";
        request.Expected = " ";

        await _service.CreateReportAsync(request, new List<FeedbackUpload>());

        var stored = await _context.FeedbackClientReports.SingleAsync();
        stored.Module.Should().BeNull();
        stored.Expected.Should().BeNull();
    }

    [Fact]
    public async Task CreateReportAsync_UngueltigeEingaben_SpeichertNichtsUndWirft()
    {
        var request = FeedbackTestData.Bug();
        request.Module = null;
        var files = new List<FeedbackUpload> { new("x.png", FeedbackTestData.Exe) };

        var act = () => _service.CreateReportAsync(request, files);

        (await act.Should().ThrowAsync<FeedbackValidationException>()).Which.Errors.Should().HaveCount(2);
        (await _context.FeedbackClientReports.CountAsync()).Should().Be(0);
        _signal.Verify(s => s.Signal(), Times.Never);
    }

    [Fact]
    public async Task GetReportsAsync_LiefertNeuesteZuerstMitKommentaren()
    {
        var older = await _service.CreateReportAsync(FeedbackTestData.Wish(), new List<FeedbackUpload>());
        await Task.Delay(10);
        await _service.CreateReportAsync(FeedbackTestData.Bug(), new List<FeedbackUpload>());
        await _service.AddReplyAsync(older.Id, "Ergänzung");

        var reports = await _service.GetReportsAsync();

        reports.Should().HaveCount(2);
        reports[1].Id.Should().Be(older.Id);
        reports[1].Comments.Single().Text.Should().Be("Ergänzung");
    }

    [Fact]
    public async Task AddReplyAsync_LegtAusstehendenKundenkommentarAn()
    {
        var report = await _service.CreateReportAsync(FeedbackTestData.Wish(), new List<FeedbackUpload>());

        await _service.AddReplyAsync(report.Id, "  Danke!  ");

        var comment = await _context.FeedbackClientComments.SingleAsync();
        comment.Text.Should().Be("Danke!");
        comment.Author.Should().Be(FeedbackCommentAuthor.Kunde);
        comment.SyncState.Should().Be(FeedbackSyncState.Ausstehend);
        comment.ClientCommentId.Should().NotBeNull();
        _signal.Verify(s => s.Signal(), Times.Exactly(2));
    }

    [Fact]
    public async Task AddReplyAsync_LeererText_Wirft()
    {
        var act = () => _service.AddReplyAsync(1, " ");

        await act.Should().ThrowAsync<FeedbackValidationException>();
    }

    [Fact]
    public async Task AddReplyAsync_UnbekannteMeldung_Wirft()
    {
        var act = () => _service.AddReplyAsync(999, "Hallo");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task TestConnectionAsync_Erfolg_NenntInstanzname()
    {
        _hubClient.Setup(c => c.GetInstanceAsync(new FeedbackHubConnection("https://hub", "wbfb_neu"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeedbackInstanceInfoDto { Name = "Tischlerei Nord" });

        var (success, message) = await _service.TestConnectionAsync(" https://hub ", " wbfb_neu ");

        success.Should().BeTrue();
        message.Should().Contain("Tischlerei Nord");
    }

    [Fact]
    public async Task TestConnectionAsync_OhneEingegebenenKey_NutztGespeichertenKey()
    {
        _settingsService.Setup(s => s.GetApiKeyAsync()).ReturnsAsync("wbfb_gespeichert");
        _hubClient.Setup(c => c.GetInstanceAsync(It.IsAny<FeedbackHubConnection>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FeedbackHubException("API-Key ungültig", false));

        var (success, message) = await _service.TestConnectionAsync("https://hub", "");

        success.Should().BeFalse();
        message.Should().Be("API-Key ungültig");
        _hubClient.Verify(c => c.GetInstanceAsync(new FeedbackHubConnection("https://hub", "wbfb_gespeichert"), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task TestConnectionAsync_OhneUrlOderKey_MeldetFehlendeAngaben()
    {
        var (success, _) = await _service.TestConnectionAsync(null, null);

        success.Should().BeFalse();
        _hubClient.VerifyNoOtherCalls();
    }
}
