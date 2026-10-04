using FluentAssertions;
using Kuestencode.Werkbank.Host.Models.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Kuestencode.Host.Tests.Services.Feedback;

public class FeedbackSyncBackgroundServiceTests
{
    private readonly FeedbackModeState _modeState = new();
    private readonly FeedbackOutboxSignal _signal = new();
    private readonly Mock<IFeedbackSyncService> _syncService = new();
    private readonly List<bool> _pollFlags = new();
    private readonly SemaphoreSlim _called = new(0);

    private FeedbackSyncBackgroundService CreateService()
    {
        _syncService.Setup(s => s.SyncAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Callback<bool, CancellationToken>((poll, _) =>
            {
                lock (_pollFlags) _pollFlags.Add(poll);
                _called.Release();
            })
            .ReturnsAsync(new FeedbackSyncResult(true, null));

        var provider = new ServiceCollection()
            .AddScoped(_ => _syncService.Object)
            .BuildServiceProvider();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Feedback:PollIntervalMinutes"] = "60" })
            .Build();

        return new FeedbackSyncBackgroundService(provider, _modeState, _signal, configuration,
            NullLogger<FeedbackSyncBackgroundService>.Instance);
    }

    [Fact]
    public async Task ClientRolle_PolltBeimStartUndSendetNachSignalOhneErneutesPolling()
    {
        _modeState.SetRole(FeedbackRole.Client);
        using var service = CreateService();

        await service.StartAsync(CancellationToken.None);
        (await _called.WaitAsync(TimeSpan.FromSeconds(5))).Should().BeTrue();
        _signal.Signal();
        (await _called.WaitAsync(TimeSpan.FromSeconds(5))).Should().BeTrue();
        await service.StopAsync(CancellationToken.None);

        lock (_pollFlags)
            _pollFlags.Take(2).Should().Equal(true, false);
    }

    [Fact]
    public async Task OhneClientRolle_RuftKeinenAbgleichAuf()
    {
        _modeState.SetRole(FeedbackRole.Hub);
        using var service = CreateService();

        await service.StartAsync(CancellationToken.None);
        _signal.Signal();
        (await _called.WaitAsync(TimeSpan.FromMilliseconds(300))).Should().BeFalse();
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FehlerImAbgleich_BeendetDenDienstNicht()
    {
        _modeState.SetRole(FeedbackRole.Client);
        using var service = CreateService();
        var calls = 0;
        _syncService.Setup(s => s.SyncAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                _called.Release();
                return Interlocked.Increment(ref calls) == 1
                    ? throw new InvalidOperationException("DB weg")
                    : Task.FromResult(new FeedbackSyncResult(true, null));
            });

        await service.StartAsync(CancellationToken.None);
        (await _called.WaitAsync(TimeSpan.FromSeconds(5))).Should().BeTrue();
        _signal.Signal();
        (await _called.WaitAsync(TimeSpan.FromSeconds(5))).Should().BeTrue();
        await service.StopAsync(CancellationToken.None);

        calls.Should().BeGreaterThanOrEqualTo(2);
    }
}
