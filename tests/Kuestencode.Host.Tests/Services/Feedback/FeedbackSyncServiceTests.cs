using FluentAssertions;
using Kuestencode.Shared.Contracts.Feedback;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Models.Feedback;
using Kuestencode.Werkbank.Host.Services;
using Kuestencode.Werkbank.Host.Services.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback.Client;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Kuestencode.Host.Tests.Services.Feedback;

public class FeedbackSyncServiceTests : IDisposable
{
    private readonly HostDbContext _context = FeedbackTestData.CreateContext();
    private readonly string _storagePath = Directory.CreateTempSubdirectory("werkbank-feedback-sync-").FullName;
    private readonly FeedbackModeState _modeState = new();
    private readonly Mock<IFeedbackHubClient> _hubClient = new();
    private readonly FeedbackSettingsService _settingsService;
    private readonly FeedbackFileStore _fileStore;
    private readonly FeedbackSyncService _service;

    public FeedbackSyncServiceTests()
    {
        _settingsService = new FeedbackSettingsService(
            _context, new PasswordEncryptionService(new EphemeralDataProtectionProvider()), _modeState);
        _fileStore = FeedbackTestData.CreateFileStore(_storagePath);
        _service = new FeedbackSyncService(
            _context, _settingsService, _modeState, _hubClient.Object, _fileStore, NullLogger<FeedbackSyncService>.Instance);

        _hubClient.Setup(c => c.GetChangesAsync(It.IsAny<FeedbackHubConnection>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeedbackSyncResponse { ServerTime = DateTime.UtcNow });
    }

    public void Dispose()
    {
        if (Directory.Exists(_storagePath))
            Directory.Delete(_storagePath, true);
    }

    private async Task ConfigureClientAsync()
    {
        var settings = await _settingsService.GetSettingsAsync();
        settings.Role = FeedbackRole.Client;
        settings.HubUrl = "https://hub.example.com";
        await _settingsService.UpdateSettingsAsync(settings, "wbfb_key");
    }

    private async Task<FeedbackClientReport> AddPendingReportAsync(int? hubReportId = null, FeedbackSyncState state = FeedbackSyncState.Ausstehend)
    {
        var report = new FeedbackClientReport
        {
            ClientReportId = Guid.NewGuid(),
            Type = FeedbackType.Bug,
            Module = "Faktura",
            Title = "Titel",
            Expected = "Soll",
            Actual = "Ist",
            ReporterName = "Erika",
            PageUrl = "/faktura",
            AppVersion = "dev",
            SyncState = state,
            HubReportId = hubReportId
        };
        _context.FeedbackClientReports.Add(report);
        await _context.SaveChangesAsync();
        return report;
    }

    private void SetupCreateReport(Func<CreateFeedbackReportRequest, FeedbackReportDto> response) =>
        _hubClient.Setup(c => c.CreateReportAsync(It.IsAny<FeedbackHubConnection>(), It.IsAny<CreateFeedbackReportRequest>(),
                It.IsAny<IReadOnlyList<(string, string, byte[])>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeedbackHubConnection _, CreateFeedbackReportRequest r, IReadOnlyList<(string, string, byte[])> _, CancellationToken _) => response(r));

    private void SetupCreateReportFails(bool permanent) =>
        _hubClient.Setup(c => c.CreateReportAsync(It.IsAny<FeedbackHubConnection>(), It.IsAny<CreateFeedbackReportRequest>(),
                It.IsAny<IReadOnlyList<(string, string, byte[])>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FeedbackHubException("Fehler", permanent));

    [Fact]
    public async Task SyncAsync_OhneClientRolle_TutNichts()
    {
        var result = await _service.SyncAsync(poll: true);

        result.Executed.Should().BeFalse();
        _hubClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SyncAsync_ClientOhneKonfiguration_MeldetFehlendeKonfiguration()
    {
        _modeState.SetRole(FeedbackRole.Client);

        var result = await _service.SyncAsync(poll: true);

        result.Executed.Should().BeFalse();
        result.Error.Should().Contain("nicht konfiguriert");
    }

    [Fact]
    public async Task SyncAsync_SendetAusstehendeMeldungMitAnhaengenUndUebernimmtHubStatus()
    {
        await ConfigureClientAsync();
        var report = await AddPendingReportAsync();
        _context.FeedbackClientAttachments.Add(new FeedbackClientAttachment
        {
            ClientReportId = report.Id, FileName = "a.png", ContentType = "image/png", Size = FeedbackTestData.Png.Length,
            StoragePath = await _fileStore.SaveAsync("outbox/x", ".png", FeedbackTestData.Png)
        });
        await _context.SaveChangesAsync();
        IReadOnlyList<(string FileName, string ContentType, byte[] Content)>? sentFiles = null;
        _hubClient.Setup(c => c.CreateReportAsync(It.IsAny<FeedbackHubConnection>(), It.IsAny<CreateFeedbackReportRequest>(),
                It.IsAny<IReadOnlyList<(string, string, byte[])>>(), It.IsAny<CancellationToken>()))
            .Callback<FeedbackHubConnection, CreateFeedbackReportRequest, IReadOnlyList<(string, string, byte[])>, CancellationToken>(
                (_, _, files, _) => sentFiles = files)
            .ReturnsAsync(new FeedbackReportDto { Id = 42, ClientReportId = report.ClientReportId, Status = FeedbackStatus.Neu });

        await _service.SyncAsync(poll: false);

        var stored = await _context.FeedbackClientReports.AsNoTracking().SingleAsync();
        stored.SyncState.Should().Be(FeedbackSyncState.Gesendet);
        stored.HubReportId.Should().Be(42);
        sentFiles!.Single().Content.Should().Equal(FeedbackTestData.Png);
        _hubClient.Verify(c => c.CreateReportAsync(
            It.Is<FeedbackHubConnection>(x => x.BaseUrl == "https://hub.example.com" && x.ApiKey == "wbfb_key"),
            It.Is<CreateFeedbackReportRequest>(r => r.ClientReportId == report.ClientReportId && r.Module == "Faktura"),
            It.IsAny<IReadOnlyList<(string, string, byte[])>>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task SyncAsync_HubNichtErreichbar_PlantRetryMitBackoff()
    {
        await ConfigureClientAsync();
        await AddPendingReportAsync();
        SetupCreateReportFails(permanent: false);

        await _service.SyncAsync(poll: false);

        var stored = await _context.FeedbackClientReports.AsNoTracking().SingleAsync();
        stored.SyncState.Should().Be(FeedbackSyncState.Ausstehend);
        stored.SendAttempts.Should().Be(1);
        stored.NextAttemptAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(1), TimeSpan.FromSeconds(10));
        stored.LastError.Should().Be("Fehler");
    }

    [Fact]
    public async Task SyncAsync_RetryNochNichtFaellig_SendetNicht()
    {
        await ConfigureClientAsync();
        await AddPendingReportAsync();
        SetupCreateReportFails(permanent: false);
        await _service.SyncAsync(poll: false);

        await _service.SyncAsync(poll: false);

        _hubClient.Verify(c => c.CreateReportAsync(It.IsAny<FeedbackHubConnection>(), It.IsAny<CreateFeedbackReportRequest>(),
            It.IsAny<IReadOnlyList<(string, string, byte[])>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncAsync_HubLehntInhaltAb_MarkiertAlsFehlgeschlagenOhneRetry()
    {
        await ConfigureClientAsync();
        await AddPendingReportAsync();
        SetupCreateReportFails(permanent: true);

        await _service.SyncAsync(poll: false);

        var stored = await _context.FeedbackClientReports.AsNoTracking().SingleAsync();
        stored.SyncState.Should().Be(FeedbackSyncState.Fehlgeschlagen);
        stored.NextAttemptAt.Should().BeNull();
    }

    [Fact]
    public async Task SyncAsync_KommentarZuNochNichtGesendeterMeldung_WartetAufMeldung()
    {
        await ConfigureClientAsync();
        var report = await AddPendingReportAsync();
        _context.FeedbackClientComments.Add(new FeedbackClientComment
        {
            ClientReportId = report.Id, ClientCommentId = Guid.NewGuid(), Author = FeedbackCommentAuthor.Kunde, Text = "Nachtrag", CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        SetupCreateReportFails(permanent: false);

        await _service.SyncAsync(poll: false);

        _hubClient.Verify(c => c.AddCommentAsync(It.IsAny<FeedbackHubConnection>(), It.IsAny<int>(),
            It.IsAny<CreateFeedbackCommentRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncAsync_SendetKommentarNachMeldungImSelbenDurchlauf()
    {
        await ConfigureClientAsync();
        var report = await AddPendingReportAsync();
        var clientCommentId = Guid.NewGuid();
        _context.FeedbackClientComments.Add(new FeedbackClientComment
        {
            ClientReportId = report.Id, ClientCommentId = clientCommentId, Author = FeedbackCommentAuthor.Kunde, Text = "Nachtrag", CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        SetupCreateReport(r => new FeedbackReportDto { Id = 42, ClientReportId = r.ClientReportId });
        _hubClient.Setup(c => c.AddCommentAsync(It.IsAny<FeedbackHubConnection>(), 42,
                It.Is<CreateFeedbackCommentRequest>(x => x.ClientCommentId == clientCommentId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeedbackCommentDto { Id = 7, ClientCommentId = clientCommentId });

        await _service.SyncAsync(poll: false);

        var comment = await _context.FeedbackClientComments.AsNoTracking().SingleAsync();
        comment.SyncState.Should().Be(FeedbackSyncState.Gesendet);
        comment.HubCommentId.Should().Be(7);
    }

    [Fact]
    public async Task SyncAsync_KommentarFehler_PlantRetry()
    {
        await ConfigureClientAsync();
        var report = await AddPendingReportAsync(hubReportId: 42, state: FeedbackSyncState.Gesendet);
        _context.FeedbackClientComments.Add(new FeedbackClientComment
        {
            ClientReportId = report.Id, ClientCommentId = Guid.NewGuid(), Author = FeedbackCommentAuthor.Kunde, Text = "x", CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        _hubClient.Setup(c => c.AddCommentAsync(It.IsAny<FeedbackHubConnection>(), 42, It.IsAny<CreateFeedbackCommentRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FeedbackHubException("offline", false));

        await _service.SyncAsync(poll: false);

        var comment = await _context.FeedbackClientComments.AsNoTracking().SingleAsync();
        comment.SyncState.Should().Be(FeedbackSyncState.Ausstehend);
        comment.SendAttempts.Should().Be(1);
        comment.NextAttemptAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SyncAsync_Polling_UebernimmtStatusDuplikatUndOeffentlicheKommentare()
    {
        await ConfigureClientAsync();
        var report = await AddPendingReportAsync(hubReportId: 42, state: FeedbackSyncState.Gesendet);
        var ownCommentId = Guid.NewGuid();
        _context.FeedbackClientComments.Add(new FeedbackClientComment
        {
            ClientReportId = report.Id, ClientCommentId = ownCommentId, Author = FeedbackCommentAuthor.Kunde, Text = "Eigener",
            CreatedAt = DateTime.UtcNow, SyncState = FeedbackSyncState.Ausstehend, NextAttemptAt = DateTime.UtcNow.AddHours(1)
        });
        await _context.SaveChangesAsync();
        var serverTime = new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc);
        var hubDto = new FeedbackReportDto
        {
            Id = 42, ClientReportId = report.ClientReportId, Status = FeedbackStatus.Abgelehnt, DuplicateOfId = 17,
            Comments = new List<FeedbackCommentDto>
            {
                new() { Id = 100, Author = FeedbackCommentAuthor.Hub, Text = "Ist ein Duplikat", CreatedAt = serverTime },
                new() { Id = 101, ClientCommentId = ownCommentId, Author = FeedbackCommentAuthor.Kunde, Text = "Eigener", CreatedAt = serverTime }
            }
        };
        _hubClient.Setup(c => c.GetChangesAsync(It.IsAny<FeedbackHubConnection>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeedbackSyncResponse { ServerTime = serverTime, Reports = new List<FeedbackReportDto> { hubDto } });

        await _service.SyncAsync(poll: true);
        await _service.SyncAsync(poll: true);

        var stored = await _context.FeedbackClientReports.AsNoTracking().Include(r => r.Comments).SingleAsync();
        stored.Status.Should().Be(FeedbackStatus.Abgelehnt);
        stored.DuplicateOfHubId.Should().Be(17);
        stored.Comments.Should().HaveCount(2);
        stored.Comments.Single(c => c.ClientCommentId == ownCommentId).Should().Match<FeedbackClientComment>(c =>
            c.HubCommentId == 101 && c.SyncState == FeedbackSyncState.Gesendet);
        stored.Comments.Single(c => c.HubCommentId == 100).Author.Should().Be(FeedbackCommentAuthor.Hub);
        (await _settingsService.GetSettingsAsync()).LastSyncAt.Should().Be(serverTime);
    }

    [Fact]
    public async Task SyncAsync_Polling_FragtMitUeberlappungSeitLetztemAbrufAb()
    {
        await ConfigureClientAsync();
        var lastSync = new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc);
        await _settingsService.SetLastSyncAtAsync(lastSync);

        await _service.SyncAsync(poll: true);

        _hubClient.Verify(c => c.GetChangesAsync(It.IsAny<FeedbackHubConnection>(), lastSync.AddMinutes(-1), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task SyncAsync_PollingFehler_WirdAlsErgebnisGemeldet()
    {
        await ConfigureClientAsync();
        _hubClient.Setup(c => c.GetChangesAsync(It.IsAny<FeedbackHubConnection>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FeedbackHubException("Hub nicht erreichbar", false));

        var result = await _service.SyncAsync(poll: true);

        result.Executed.Should().BeTrue();
        result.Error.Should().Be("Hub nicht erreichbar");
    }

    [Fact]
    public async Task SyncAsync_UnbekannteMeldungVomHub_WirdIgnoriert()
    {
        await ConfigureClientAsync();
        _hubClient.Setup(c => c.GetChangesAsync(It.IsAny<FeedbackHubConnection>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeedbackSyncResponse
            {
                ServerTime = DateTime.UtcNow,
                Reports = new List<FeedbackReportDto> { new() { Id = 1, ClientReportId = Guid.NewGuid() } }
            });

        var result = await _service.SyncAsync(poll: true);

        result.Error.Should().BeNull();
        (await _context.FeedbackClientReports.CountAsync()).Should().Be(0);
    }
}
