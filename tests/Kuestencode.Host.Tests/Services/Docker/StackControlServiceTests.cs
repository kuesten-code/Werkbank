using FluentAssertions;
using Kuestencode.Werkbank.Host.Services.Docker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Kuestencode.Host.Tests.Services.Docker;

public class StackControlServiceTests
{
    private readonly Mock<IDockerControlService> _docker = new();
    private readonly Mock<IModuleControlService> _modules = new();
    private readonly StackControlService _service;
    private readonly List<string> _calls = new();

    public StackControlServiceTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DockerControl:ContainerNamePattern"] = "wb_{0}",
                ["DockerControl:PostgresStartTimeoutSeconds"] = "1",
                ["DockerControl:PollIntervalMilliseconds"] = "10",
            })
            .Build();

        _service = new StackControlService(_docker.Object, _modules.Object, configuration, NullLogger<StackControlService>.Instance);

        _docker.Setup(d => d.StopAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<string, int, CancellationToken>((name, _, _) => _calls.Add($"stop:{name}"))
            .Returns(Task.CompletedTask);
        _docker.Setup(d => d.StartAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((name, _) => _calls.Add($"start:{name}"))
            .Returns(Task.CompletedTask);
    }

    private static ModuleStatus Module(string key, bool running, params string[] requires) =>
        new(new ManagedModule(key, key, requires, null), $"wb_{key}", new ContainerState(running ? "running" : "exited", null));

    private void SetPostgres(ContainerState? state) =>
        _docker.Setup(d => d.GetStateAsync("wb_postgres", It.IsAny<CancellationToken>())).ReturnsAsync(state);

    private void SetModules(params ModuleStatus[] modules) =>
        _modules.Setup(m => m.GetStatusAsync(It.IsAny<CancellationToken>())).ReturnsAsync(modules.ToList());

    [Fact]
    public async Task StopAsync_StopptErstLaufendeModuleDannPostgresUndMerktSichNurWasLief()
    {
        SetPostgres(new ContainerState("running", "healthy"));
        SetModules(Module("faktura", true), Module("acta", false), Module("saldo", true, "faktura", "recepta"));

        var snapshot = await _service.StopAsync();

        _calls.Should().Equal("stop:wb_faktura", "stop:wb_saldo", "stop:wb_postgres");
        snapshot.PostgresWasRunning.Should().BeTrue();
        snapshot.RunningModules.Select(m => m.Key).Should().Equal("faktura", "saldo");
    }

    [Fact]
    public async Task StopAsync_PostgresContainerFehlt_WirftOhneetwasZuStoppen()
    {
        SetPostgres(null);
        SetModules(Module("faktura", true));

        var act = async () => await _service.StopAsync();

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*wb_postgres*nicht gefunden*");
        _calls.Should().BeEmpty();
    }

    [Fact]
    public async Task StopAsync_PostgresLaeuftNichtBereits_WirdNichtGestoppt()
    {
        SetPostgres(new ContainerState("exited", null));
        SetModules(Module("faktura", true));

        var snapshot = await _service.StopAsync();

        snapshot.PostgresWasRunning.Should().BeFalse();
        _calls.Should().Equal("stop:wb_faktura");
    }

    [Fact]
    public async Task StopAsync_ModulLaesstSichNichtStoppen_StartetBereitsGestopptesWiederUndWirft()
    {
        SetPostgres(new ContainerState("running", "healthy"));
        SetModules(Module("faktura", true), Module("rapport", true));
        _docker.Setup(d => d.StopAsync("wb_rapport", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        _docker.SetupSequence(d => d.GetStateAsync("wb_postgres", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContainerState("running", "healthy"))
            .ReturnsAsync(new ContainerState("running", "healthy"));

        var act = async () => await _service.StopAsync();

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
        _calls.Should().Contain("start:wb_faktura").And.Contain("start:wb_rapport").And.Contain("start:wb_postgres");
    }

    [Fact]
    public async Task StartAsync_StartetPostgresWartetAufGesundUndStartetModuleMitAbhaengigkeitenZuletzt()
    {
        _docker.SetupSequence(d => d.GetStateAsync("wb_postgres", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContainerState("running", "starting"))
            .ReturnsAsync(new ContainerState("running", "healthy"));
        var snapshot = new StackSnapshot(true, new List<StackModule>
        {
            new("saldo", "wb_saldo", 2),
            new("faktura", "wb_faktura", 0),
        });

        await _service.StartAsync(snapshot);

        _calls.Should().Equal("start:wb_postgres", "start:wb_faktura", "start:wb_saldo");
    }

    [Fact]
    public async Task StartAsync_PostgresWirdNichtGesund_WirftUndStartetKeineModule()
    {
        _docker.Setup(d => d.GetStateAsync("wb_postgres", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContainerState("restarting", "unhealthy"));
        var snapshot = new StackSnapshot(true, new List<StackModule> { new("faktura", "wb_faktura", 0) });

        var act = async () => await _service.StartAsync(snapshot);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*nicht gesund*restarting*unhealthy*");
        _calls.Should().Equal("start:wb_postgres");
    }

    [Fact]
    public async Task StartAsync_EinzelnesModulStartetNicht_KippenDenVorgangNicht()
    {
        _docker.Setup(d => d.GetStateAsync("wb_postgres", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContainerState("running", "healthy"));
        _docker.Setup(d => d.StartAsync("wb_faktura", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var snapshot = new StackSnapshot(true, new List<StackModule>
        {
            new("faktura", "wb_faktura", 0),
            new("rapport", "wb_rapport", 0),
        });

        var act = async () => await _service.StartAsync(snapshot);

        await act.Should().NotThrowAsync();
        _calls.Should().Contain("start:wb_rapport");
    }

    [Fact]
    public async Task StartAsync_PostgresLiefVorherNicht_LaesstIhnAusUndStartetNurModule()
    {
        var snapshot = new StackSnapshot(false, new List<StackModule> { new("faktura", "wb_faktura", 0) });

        await _service.StartAsync(snapshot);

        _calls.Should().Equal("start:wb_faktura");
    }
}
