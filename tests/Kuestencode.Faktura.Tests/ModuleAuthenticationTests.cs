using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Kuestencode.Faktura.Tests.TestDoubles;
using Kuestencode.Shared.UI.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Kuestencode.Faktura.Tests;

/// <summary>
/// Security-Audit H4 und K1 (Modulschicht): das Modul verlangt auch bei direktem Zugriff, an Host und
/// Proxy vorbei, ein signiertes Token, sowohl für die API als auch für die Blazor-Seiten.
/// </summary>
public class ModuleAuthenticationTests : IClassFixture<FakturaWebApplicationFactory>
{
    private readonly FakturaWebApplicationFactory _factory;

    public ModuleAuthenticationTests(FakturaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient Client(string? bearer = null)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        if (bearer != null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        return client;
    }

    [Fact]
    public async Task Api_MitServiceToken_IstErlaubt()
    {
        var serviceToken = new ServiceTokenProvider(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Secret"] = TestJwt.Secret })
            .Build()).GetToken();

        var response = await Client(serviceToken).GetAsync("/api/Invoice");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("/faktura/invoices")]
    [InlineData("/faktura/")]
    public async Task Seite_OhneToken_LeitetZumHostLoginUm(string path)
    {
        var response = await Client().GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.PathAndQuery.Should().StartWith("/login?returnUrl=%2Ffaktura");
    }

    [Fact]
    public async Task Seite_MitGefaelschtemToken_LeitetZumHostLoginUm()
    {
        var forged = TestJwt.Create("Admin", secret: "attacker-chosen-secret-0123456789abcdef");

        var response = await Client(forged).GetAsync("/faktura/invoices");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task Seite_MitGueltigemToken_WirdGerendert()
    {
        var response = await Client(TestJwt.Create("Admin")).GetAsync("/faktura/invoices");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("Mitarbeiter", "/faktura/invoices", true)]
    [InlineData("Buero", "/faktura/invoices", false)]
    [InlineData("Buero", "/faktura/settings/pdf-anpassung", true)]
    [InlineData("Admin", "/faktura/settings/pdf-anpassung", false)]
    public async Task Seite_RolleOhneBerechtigung_ZeigtZugriffVerweigert(string role, string path, bool denied)
    {
        var response = await Client(TestJwt.Create(role)).GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        html.Contains("Zugriff verweigert").Should().Be(denied);
    }
}
