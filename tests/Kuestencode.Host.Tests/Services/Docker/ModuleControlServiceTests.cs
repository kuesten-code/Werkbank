using FluentAssertions;
using Kuestencode.Shared.Contracts.Navigation;
using Kuestencode.Werkbank.Host.Services;
using Kuestencode.Werkbank.Host.Services.Docker;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Kuestencode.Host.Tests.Services.Docker;

public class ModuleControlServiceTests
{
    private readonly Mock<IDockerControlService> _docker = new();
    private readonly ModuleRegistry _registry = new();
    private readonly Mock<IManuallyStoppedModules> _manuallyStopped = new();

    private ModuleControlService CreateService(params string[] modules) => CreateService(modules, new Dictionary<string, string?>());

    private ModuleControlService CreateService(string[] modules, Dictionary<string, string?> extra)
    {
        var settings = new Dictionary<string, string?>
        {
            ["DockerControl:ContainerNamePattern"] = "werkbank_{0}",
            ["ModuleControl:Requires:Saldo"] = "Faktura,Recepta",
            ["ModuleControl:StopHints:Rapport"] = "Acta kann dann keine Stunden von Mitarbeitern erfassen.",
        };
        foreach (var module in modules)
            settings[$"ServiceUrls:{module}"] = $"http://{module.ToLowerInvariant()}:8080";
        foreach (var (key, value) in extra)
            settings[key] = value;

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new ModuleControlService(_docker.Object, configuration, _registry, _manuallyStopped.Object);
    }

    private void SetStates(params (string Module, string? Status)[] states)
    {
        foreach (var (module, status) in states)
        {
            _docker.Setup(d => d.GetStateAsync($"werkbank_{module}", It.IsAny<CancellationToken>()))
                .ReturnsAsync(status == null ? null : new ContainerState(status, null));
        }
    }

    private static readonly string[] AllModules = { "Faktura", "Rapport", "Offerte", "Acta", "Recepta", "Saldo" };

    private static readonly (string, string?)[] AllRunning =
    {
        ("faktura", "running"), ("rapport", "running"), ("offerte", "running"),
        ("acta", "running"), ("recepta", "running"), ("saldo", "running"),
    };

    [Fact]
    public async Task GetStatusAsync_ListeFolgtDerKonfiguration()
    {
        SetStates(("faktura", "running"), ("saldo", "exited"));
        var service = CreateService("Faktura", "Saldo");

        var statuses = await service.GetStatusAsync();

        statuses.Select(s => s.Module.Key).Should().Equal("Faktura", "Saldo");
        statuses.Select(s => s.ContainerName).Should().Equal("werkbank_faktura", "werkbank_saldo");
    }

    [Fact]
    public async Task GetStatusAsync_ModulOhneContainerInDerCompose_TauchtNichtAuf()
    {
        SetStates(("faktura", "running"), ("rapport", null), ("saldo", "running"));
        var service = CreateService("Faktura", "Rapport", "Saldo");

        var statuses = await service.GetStatusAsync();

        statuses.Select(s => s.Module.Key).Should().Equal("Faktura", "Saldo");
    }

    [Fact]
    public async Task GetStatusAsync_NutztAnzeigenamenAusDerRegistry_SonstDenSchluessel()
    {
        _registry.RegisterModule(new ModuleInfoDto { ModuleName = "Faktura", DisplayName = "Rechnungen" });
        SetStates(("faktura", "running"), ("acta", "running"));
        var service = CreateService("Faktura", "Acta");

        var statuses = await service.GetStatusAsync();

        statuses.Select(s => s.Module.DisplayName).Should().Equal("Acta", "Rechnungen");
    }

    [Fact]
    public async Task GetStatusAsync_UnbekannteZusatzmodule_WerdenOhneCodeaenderungAufgenommen()
    {
        SetStates(("faktura", "running"), ("neuesmodul", "running"));
        var service = CreateService("Faktura", "NeuesModul");

        var statuses = await service.GetStatusAsync();

        statuses.Should().HaveCount(2);
        statuses[1].ContainerName.Should().Be("werkbank_neuesmodul");
        statuses[1].Module.Requires.Should().BeEmpty();
    }

    [Fact]
    public async Task StartAsync_ModulOhneAbhaengigkeit_StartetContainer()
    {
        SetStates(AllRunning);
        var service = CreateService(AllModules);

        await service.StartAsync("Offerte");

        _docker.Verify(d => d.StartAsync("werkbank_offerte", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_SaldoOhneLaufendesFaktura_WirdBlockiert()
    {
        SetStates(("faktura", "exited"), ("recepta", "running"), ("saldo", "exited"));
        var service = CreateService("Faktura", "Recepta", "Saldo");

        var act = async () => await service.StartAsync("Saldo");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Saldo benötigt Faktura.*");
        _docker.Verify(d => d.StartAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartAsync_SaldoOhneFakturaUndRecepta_NenntBeideModule()
    {
        SetStates(("faktura", "exited"), ("recepta", "exited"), ("saldo", "exited"));
        var service = CreateService("Faktura", "Recepta", "Saldo");

        var act = async () => await service.StartAsync("Saldo");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Saldo benötigt Faktura und Recepta.*");
    }

    [Fact]
    public async Task StartAsync_SaldoWennFakturaNichtInstalliert_WirdBlockiertUndNenntDenSchluessel()
    {
        SetStates(("faktura", null), ("recepta", "running"), ("saldo", "exited"));
        var service = CreateService("Faktura", "Recepta", "Saldo");

        var act = async () => await service.StartAsync("Saldo");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Saldo benötigt Faktura.*");
    }

    [Fact]
    public async Task StartAsync_SaldoMitFakturaUndRecepta_StartetContainer()
    {
        SetStates(("faktura", "running"), ("recepta", "running"), ("saldo", "exited"));
        var service = CreateService("Faktura", "Recepta", "Saldo");

        await service.StartAsync("Saldo");

        _docker.Verify(d => d.StartAsync("werkbank_saldo", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_AbhaengigkeitenLassenSichPerKonfigurationAendern()
    {
        SetStates(("faktura", "exited"), ("saldo", "exited"));
        var service = CreateService(new[] { "Faktura", "Saldo" },
            new Dictionary<string, string?> { ["ModuleControl:Requires:Saldo"] = "" });

        await service.StartAsync("Saldo");

        _docker.Verify(d => d.StartAsync("werkbank_saldo", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StopAsync_StopptContainerOhneBlockade()
    {
        SetStates(("faktura", "running"));
        var service = CreateService("Faktura");

        await service.StopAsync("faktura");

        _docker.Verify(d => d.StopAsync("werkbank_faktura", It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StopAsync_MerktSichDasModulAlsManuellGestopptUndMeldetEsInDerRegistryAb()
    {
        _registry.RegisterModule(new ModuleInfoDto { ModuleName = "Faktura", DisplayName = "Faktura" });
        SetStates(("faktura", "running"));
        var service = CreateService("Faktura");

        await service.StopAsync("Faktura");

        _manuallyStopped.Verify(m => m.Mark("Faktura", "Faktura"), Times.Once);
        _registry.GetAllModulesWithStatus().Should().ContainSingle().Which.IsOnline.Should().BeFalse();
    }

    [Fact]
    public async Task StopAsync_DockerFehlschlag_MerktNichts()
    {
        SetStates(("faktura", "running"));
        _docker.Setup(d => d.StopAsync("werkbank_faktura", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var service = CreateService("Faktura");

        var act = async () => await service.StopAsync("Faktura");

        await act.Should().ThrowAsync<InvalidOperationException>();
        _manuallyStopped.Verify(m => m.Mark(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task StartAsync_VergisstDenManuellGestopptMerker()
    {
        SetStates(("offerte", "exited"));
        var service = CreateService("Offerte");

        await service.StartAsync("Offerte");

        _manuallyStopped.Verify(m => m.Clear("Offerte"), Times.Once);
    }

    [Fact]
    public async Task StartUndStop_NichtKonfiguriertesModul_WirftArgumentException()
    {
        SetStates(AllRunning);
        var service = CreateService(AllModules);

        var start = async () => await service.StartAsync("postgres");
        var stop = async () => await service.StopAsync("host");

        await start.Should().ThrowAsync<ArgumentException>();
        await stop.Should().ThrowAsync<ArgumentException>();
        _docker.Verify(d => d.StopAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("Faktura")]
    [InlineData("Recepta")]
    public async Task GetStopWarnings_FakturaOderReceptaBeiLaufendemSaldo_WarntVorSaldo(string module)
    {
        SetStates(AllRunning);
        var statuses = await CreateService(AllModules).GetStatusAsync();

        var warnings = ModuleControlService.GetStopWarnings(module, statuses);

        warnings.Should().ContainSingle().Which.Should().Contain("Saldo benötigt");
    }

    [Fact]
    public async Task GetStopWarnings_FakturaBeiGestopptemSaldo_WarntNicht()
    {
        SetStates(("faktura", "running"), ("recepta", "running"), ("saldo", "exited"));
        var statuses = await CreateService("Faktura", "Recepta", "Saldo").GetStatusAsync();

        ModuleControlService.GetStopWarnings("Faktura", statuses).Should().BeEmpty();
    }

    [Fact]
    public async Task GetStopWarnings_Rapport_GibtNurLosenHinweisFuerActa()
    {
        SetStates(AllRunning);
        var statuses = await CreateService(AllModules).GetStatusAsync();

        var warnings = ModuleControlService.GetStopWarnings("Rapport", statuses);

        warnings.Should().ContainSingle().Which.Should().Contain("Acta").And.Contain("Mitarbeitern");
    }

    [Fact]
    public async Task GetStopWarnings_ModulOhneAbhaengigkeiten_HatKeineWarnungen()
    {
        SetStates(AllRunning);
        var statuses = await CreateService(AllModules).GetStatusAsync();

        ModuleControlService.GetStopWarnings("Offerte", statuses).Should().BeEmpty();
    }
}
