using FluentAssertions;
using Kuestencode.Shared.Contracts.Navigation;
using Kuestencode.Werkbank.Host.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Kuestencode.Host.Tests.Services;

/// <summary>
/// Security-Audit H2: registrierte Daten dürfen weder SSRF noch Phishing-Links ermöglichen.
/// </summary>
public class ModuleRegistrationValidatorTests
{
    private static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["ServiceUrls:Faktura"] = "http://faktura:8081" })
        .Build();

    private static ModuleInfoDto Valid() => new()
    {
        ModuleName = "Faktura",
        HealthCheckUrl = "http://faktura:8081/faktura/health",
        LogoUrl = "/faktura/company/logos/Faktura_Logo.png",
        NavigationItems =
        [
            new NavItemDto
            {
                Label = "Faktura",
                Href = "/faktura",
                Icon = "",
                Children = [new NavItemDto { Label = "Rechnungen", Href = "/faktura/invoices" }]
            }
        ]
    };

    [Fact]
    public void Validate_EchteModulRegistrierung_IstGueltig()
    {
        ModuleRegistrationValidator.Validate(Valid(), Configuration).Should().BeNull();
    }

    [Fact]
    public void Validate_OhneHealthCheckUrl_IstGueltig()
    {
        ModuleRegistrationValidator.Validate(Valid() with { HealthCheckUrl = null }, Configuration).Should().BeNull();
    }

    [Fact]
    public void Validate_UnbekanntesModul_WirdAbgelehnt()
    {
        ModuleRegistrationValidator.Validate(Valid() with { ModuleName = "Evil" }, Configuration)
            .Should().Contain("ServiceUrl");
    }

    [Theory]
    [InlineData("http://docker-control:2375/containers/json")]
    [InlineData("http://169.254.169.254/latest/meta-data")]
    [InlineData("http://faktura:9999/faktura/health")]
    [InlineData("https://faktura:8081/faktura/health")]
    [InlineData("kein-url")]
    public void Validate_HealthCheckUrlAufFremdesZiel_WirdAbgelehnt(string url)
    {
        ModuleRegistrationValidator.Validate(Valid() with { HealthCheckUrl = url }, Configuration)
            .Should().Contain("HealthCheckUrl");
    }

    [Theory]
    [InlineData("https://phishing.example/login")]
    [InlineData("//phishing.example")]
    [InlineData("/\\phishing.example")]
    [InlineData("javascript:alert(1)")]
    public void Validate_ExterneLogoUrl_WirdAbgelehnt(string url)
    {
        ModuleRegistrationValidator.Validate(Valid() with { LogoUrl = url }, Configuration)
            .Should().Contain("LogoUrl");
    }

    [Fact]
    public void Validate_ExternerLinkInUntermenue_WirdAbgelehnt()
    {
        var info = Valid();
        info.NavigationItems[0].Children.Add(new NavItemDto { Label = "Böse", Href = "https://phishing.example" });

        ModuleRegistrationValidator.Validate(info, Configuration).Should().Contain("Böse");
    }

    [Fact]
    public void Validate_ExternesIcon_WirdAbgelehnt()
    {
        var info = Valid();
        info.NavigationItems.Add(new NavItemDto { Label = "Icon", Href = "/faktura", Icon = "https://tracker.example/pixel.png" });

        ModuleRegistrationValidator.Validate(info, Configuration).Should().Contain("Icon");
    }
}
