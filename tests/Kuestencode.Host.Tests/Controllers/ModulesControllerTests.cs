using FluentAssertions;
using Kuestencode.Shared.Contracts.Navigation;
using Kuestencode.Werkbank.Host.Controllers;
using Kuestencode.Werkbank.Host.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Kuestencode.Host.Tests.Controllers;

public class ModulesControllerTests
{
    private readonly ModuleRegistry _registry = new();
    private readonly ModulesController _controller;

    public ModulesControllerTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ServiceUrls:Faktura"] = "http://faktura:8081" })
            .Build();
        _controller = new ModulesController(_registry, configuration, NullLogger<ModulesController>.Instance);
    }

    private static ModuleInfoDto Faktura(string healthCheckUrl = "http://faktura:8081/faktura/health") => new()
    {
        ModuleName = "Faktura",
        HealthCheckUrl = healthCheckUrl,
        NavigationItems = [new NavItemDto { Label = "Faktura", Href = "/faktura" }]
    };

    [Fact]
    public void RegisterModule_GueltigeRegistrierung_LandetInRegistry()
    {
        var result = _controller.RegisterModule(Faktura());

        result.Should().BeOfType<OkObjectResult>();
        _registry.GetModule("Faktura").Should().NotBeNull();
    }

    [Fact]
    public void RegisterModule_HealthCheckAufFremdesZiel_WirdAbgelehntUndUeberschreibtNichts()
    {
        _controller.RegisterModule(Faktura());

        var result = _controller.RegisterModule(Faktura(healthCheckUrl: "http://attacker/"));

        result.Should().BeOfType<BadRequestObjectResult>();
        _registry.GetModule("Faktura")!.HealthCheckUrl.Should().Be("http://faktura:8081/faktura/health");
    }

    [Fact]
    public void UnregisterModule_MarkiertModulAlsOffline()
    {
        _controller.RegisterModule(Faktura());

        _controller.UnregisterModule("Faktura").Should().BeOfType<OkObjectResult>();

        _registry.GetAllModules().Should().BeEmpty();
    }

    [Fact]
    public void GetModule_Unbekannt_Gibt404()
    {
        _controller.GetModule("Gibtsnicht").Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public void GetRegisteredModules_LiefertRegistrierteModule()
    {
        _controller.RegisterModule(Faktura());

        var result = _controller.GetRegisteredModules().Result.Should().BeOfType<OkObjectResult>().Subject;

        result.Value.Should().BeAssignableTo<List<ModuleInfoDto>>().Which.Should().ContainSingle();
    }
}
