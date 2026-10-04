using FluentAssertions;
using Kuestencode.Shared.Contracts.Feedback;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Models.Feedback;
using Kuestencode.Werkbank.Host.Services;
using Kuestencode.Werkbank.Host.Services.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback.Hub;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Kuestencode.Host.Tests.Services.Feedback;

public class FeedbackHubApiServiceTests : IDisposable
{
    private readonly HostDbContext _context = FeedbackTestData.CreateContext();
    private readonly string _storagePath = Directory.CreateTempSubdirectory("werkbank-feedback-hub-").FullName;
    private readonly Mock<IFeedbackNotifier> _notifier = new();
    private readonly FeedbackHubApiService _service;
    private readonly FeedbackInstance _instanceA;
    private readonly FeedbackInstance _instanceB;

    public FeedbackHubApiServiceTests()
    {
        var settingsService = new FeedbackSettingsService(
            _context, new PasswordEncryptionService(new EphemeralDataProtectionProvider()), new FeedbackModeState());

        _service = new FeedbackHubApiService(
            _context, settingsService, FeedbackTestData.CreateFileStore(_storagePath), _notifier.Object,
            NullLogger<FeedbackHubApiService>.Instance);

        _instanceA = new FeedbackInstance { Name = "Kunde A", ApiKeyHash = "a" };
        _instanceB = new FeedbackInstance { Name = "Kunde B", ApiKeyHash = "b" };
        _context.FeedbackInstances.AddRange(_instanceA, _instanceB);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        if (Directory.Exists(_storagePath))
            Directory.Delete(_storagePath, true);
    }

    private static List<FeedbackUpload> NoFiles() => new();

    // ─── Idempotenz ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateReportAsync_GleicheClientReportIdZweimal_LegtNurEineMeldungAn()
    {
        var request = FeedbackTestData.Bug();
        var uploads = new List<FeedbackUpload> { new("screen.png", FeedbackTestData.Png) };

        var (first, firstCreated) = await _service.CreateReportAsync(_instanceA.Id, request, uploads);
        var (second, secondCreated) = await _service.CreateReportAsync(_instanceA.Id, request, uploads);

        firstCreated.Should().BeTrue();
        secondCreated.Should().BeFalse();
        second.Id.Should().Be(first.Id);
        (await _context.FeedbackReports.CountAsync()).Should().Be(1);
        (await _context.FeedbackAttachments.CountAsync()).Should().Be(1);
        Directory.GetFiles(_storagePath, "*", SearchOption.AllDirectories).Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateReportAsync_Wiederholung_SendetKeineZweiteBenachrichtigung()
    {
        var request = FeedbackTestData.Wish();

        await _service.CreateReportAsync(_instanceA.Id, request, NoFiles());
        await _service.CreateReportAsync(_instanceA.Id, request, NoFiles());

        _notifier.Verify(n => n.NotifyNewReportAsync(It.IsAny<FeedbackReport>(), "Kunde A"), Times.Once);
    }

    [Fact]
    public async Task CreateReportAsync_GleicheClientReportIdBeiAndererInstanz_IstEigeneMeldung()
    {
        var clientReportId = Guid.NewGuid();

        var (reportA, _) = await _service.CreateReportAsync(_instanceA.Id, FeedbackTestData.Bug(clientReportId), NoFiles());
        var (reportB, createdB) = await _service.CreateReportAsync(_instanceB.Id, FeedbackTestData.Bug(clientReportId), NoFiles());

        createdB.Should().BeTrue();
        reportB.Id.Should().NotBe(reportA.Id);
    }

    // ─── Validierung ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateReportAsync_WunschOhneModulUndErwartung_WirdAngenommen()
    {
        var (report, created) = await _service.CreateReportAsync(_instanceA.Id, FeedbackTestData.Wish(), NoFiles());

        created.Should().BeTrue();
        report.Module.Should().BeNull();
        var stored = await _context.FeedbackReports.SingleAsync();
        stored.Module.Should().BeNull();
        stored.Expected.Should().BeNull();
        stored.Status.Should().Be(FeedbackStatus.Neu);
    }

    [Fact]
    public async Task CreateReportAsync_BugOhneModul_WirftValidierungsfehler()
    {
        var request = FeedbackTestData.Bug();
        request.Module = " ";

        var act = () => _service.CreateReportAsync(_instanceA.Id, request, NoFiles());

        (await act.Should().ThrowAsync<FeedbackValidationException>())
            .Which.Errors.Should().Contain("Modul ist bei einem Bug erforderlich.");
        (await _context.FeedbackReports.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CreateReportAsync_UnerlaubterDateityp_WirftValidierungsfehlerUndSpeichertNichts()
    {
        var uploads = new List<FeedbackUpload> { new("virus.png", FeedbackTestData.Exe) };

        var act = () => _service.CreateReportAsync(_instanceA.Id, FeedbackTestData.Bug(), uploads);

        await act.Should().ThrowAsync<FeedbackValidationException>();
        Directory.GetFiles(_storagePath, "*", SearchOption.AllDirectories).Should().BeEmpty();
    }

    [Fact]
    public async Task CreateReportAsync_DateiUeberKonfiguriertemLimit_WirdAbgelehnt()
    {
        _context.FeedbackSettings.Add(new FeedbackSettings { MaxFileSizeMb = 1, MaxReportSizeMb = 2 });
        await _context.SaveChangesAsync();
        var big = FeedbackTestData.Png.Concat(new byte[1024 * 1024]).ToArray();

        var act = () => _service.CreateReportAsync(_instanceA.Id, FeedbackTestData.Bug(), new List<FeedbackUpload> { new("big.png", big) });

        (await act.Should().ThrowAsync<FeedbackValidationException>())
            .Which.Errors.Should().ContainMatch("*größer als 1 MB*");
    }

    [Fact]
    public async Task CreateReportAsync_SpeichertDateiUnterServerNamenStattOriginalname()
    {
        var uploads = new List<FeedbackUpload> { new("../../etc/passwd.png", FeedbackTestData.Png) };

        await _service.CreateReportAsync(_instanceA.Id, FeedbackTestData.Bug(), uploads);

        var attachment = await _context.FeedbackAttachments.SingleAsync();
        attachment.FileName.Should().Be("passwd.png");
        attachment.ContentType.Should().Be("image/png");
        attachment.StoragePath.Should().NotContain("passwd").And.NotContain("..");
    }

    // ─── Instanz-Isolation ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetChangesAsync_LiefertNurMeldungenDerEigenenInstanz()
    {
        await _service.CreateReportAsync(_instanceA.Id, FeedbackTestData.Bug(), NoFiles());
        await _service.CreateReportAsync(_instanceB.Id, FeedbackTestData.Bug(), NoFiles());
        await _service.CreateReportAsync(_instanceB.Id, FeedbackTestData.Wish(), NoFiles());

        var changesA = await _service.GetChangesAsync(_instanceA.Id, null);
        var changesB = await _service.GetChangesAsync(_instanceB.Id, null);

        changesA.Reports.Should().HaveCount(1);
        changesB.Reports.Should().HaveCount(2);
        var idsOfB = (await _context.FeedbackReports.Where(r => r.InstanceId == _instanceB.Id).Select(r => r.Id).ToListAsync());
        changesA.Reports.Select(r => r.Id).Should().NotIntersectWith(idsOfB);
    }

    [Fact]
    public async Task AddCustomerCommentAsync_AufMeldungEinerAnderenInstanz_LiefertNull()
    {
        var (reportOfB, _) = await _service.CreateReportAsync(_instanceB.Id, FeedbackTestData.Bug(), NoFiles());

        var result = await _service.AddCustomerCommentAsync(_instanceA.Id, reportOfB.Id,
            new CreateFeedbackCommentRequest { ClientCommentId = Guid.NewGuid(), Text = "Hallo" });

        result.Should().BeNull();
        (await _context.FeedbackComments.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CreateReportAsync_BekannteClientReportIdEinerAnderenInstanz_LiefertNichtDeren_Meldung()
    {
        var clientReportId = Guid.NewGuid();
        var requestOfB = FeedbackTestData.Bug(clientReportId);
        requestOfB.Title = "Geheim von B";
        await _service.CreateReportAsync(_instanceB.Id, requestOfB, NoFiles());

        var (report, _) = await _service.CreateReportAsync(_instanceA.Id, FeedbackTestData.Bug(clientReportId), NoFiles());

        report.Title.Should().NotBe("Geheim von B");
    }

    // ─── Interne Kommentare ────────────────────────────────────────────────────

    [Fact]
    public async Task GetChangesAsync_BlendetInterneKommentareAus()
    {
        var (report, _) = await _service.CreateReportAsync(_instanceA.Id, FeedbackTestData.Bug(), NoFiles());
        _context.FeedbackComments.AddRange(
            new FeedbackComment { ReportId = report.Id, Author = FeedbackCommentAuthor.Hub, Text = "Öffentlich", CreatedAt = DateTime.UtcNow },
            new FeedbackComment { ReportId = report.Id, Author = FeedbackCommentAuthor.Hub, Text = "Nur intern", IsInternal = true, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        var changes = await _service.GetChangesAsync(_instanceA.Id, null);

        changes.Reports.Single().Comments.Select(c => c.Text).Should().Equal("Öffentlich");
    }

    [Fact]
    public async Task CreateReportAsync_IdempotenteAntwort_EnthaeltKeineInternenKommentare()
    {
        var request = FeedbackTestData.Bug();
        var (report, _) = await _service.CreateReportAsync(_instanceA.Id, request, NoFiles());
        _context.FeedbackComments.Add(new FeedbackComment
        {
            ReportId = report.Id, Author = FeedbackCommentAuthor.Hub, Text = "Nur intern", IsInternal = true, CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var (again, _) = await _service.CreateReportAsync(_instanceA.Id, request, NoFiles());

        again.Comments.Should().BeEmpty();
    }

    // ─── since / Kommentare ───────────────────────────────────────────────────

    [Fact]
    public async Task GetChangesAsync_MitSince_LiefertNurSpaeterGeaenderteMeldungen()
    {
        var (unchanged, _) = await _service.CreateReportAsync(_instanceA.Id, FeedbackTestData.Bug(), NoFiles());
        var (commented, _) = await _service.CreateReportAsync(_instanceA.Id, FeedbackTestData.Wish(), NoFiles());
        var since = (await _service.GetChangesAsync(_instanceA.Id, null)).ServerTime;
        await Task.Delay(20);

        await _service.AddCustomerCommentAsync(_instanceA.Id, commented.Id,
            new CreateFeedbackCommentRequest { ClientCommentId = Guid.NewGuid(), Text = "Nachtrag" });
        var changes = await _service.GetChangesAsync(_instanceA.Id, since);

        changes.ServerTime.Should().BeAfter(since);
        changes.Reports.Select(r => r.Id).Should().Equal(commented.Id);
        changes.Reports.Should().NotContain(r => r.Id == unchanged.Id);
    }

    [Fact]
    public async Task AddCustomerCommentAsync_GleicheClientCommentIdZweimal_LegtNurEinenKommentarAn()
    {
        var (report, _) = await _service.CreateReportAsync(_instanceA.Id, FeedbackTestData.Bug(), NoFiles());
        var request = new CreateFeedbackCommentRequest { ClientCommentId = Guid.NewGuid(), Text = "Tritt weiterhin auf" };

        var first = await _service.AddCustomerCommentAsync(_instanceA.Id, report.Id, request);
        var second = await _service.AddCustomerCommentAsync(_instanceA.Id, report.Id, request);

        second!.Id.Should().Be(first!.Id);
        var comment = await _context.FeedbackComments.SingleAsync();
        comment.Author.Should().Be(FeedbackCommentAuthor.Kunde);
        comment.IsInternal.Should().BeFalse();
    }

    [Fact]
    public async Task AddCustomerCommentAsync_LeererText_WirftValidierungsfehler()
    {
        var (report, _) = await _service.CreateReportAsync(_instanceA.Id, FeedbackTestData.Bug(), NoFiles());

        var act = () => _service.AddCustomerCommentAsync(_instanceA.Id, report.Id, new CreateFeedbackCommentRequest { Text = "" });

        await act.Should().ThrowAsync<FeedbackValidationException>();
    }
}
