using System.Net;
using FluentAssertions;
using Xunit;

namespace Kuestencode.Werkbank.Locatio.Tests;

/// <summary>
/// Modul-Controller müssen unauthentifizierte Requests ablehnen (siehe Recepta/Saldo-Regressionstests).
/// </summary>
public class ControllerAuthorizationTests : IClassFixture<LocatioWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ControllerAuthorizationTests(LocatioWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/api/locatio/saldo-buchungen?from=2026-01-01&to=2026-12-31")]
    [InlineData("/api/locatio/dokumente/6f1c1b1e-0000-4000-8000-000000000001")]
    [InlineData("/api/locatio/veraeusserungsgewinn/einheit/6f1c1b1e-0000-4000-8000-000000000001.pdf")]
    public async Task OhneToken_GibtUnauthorizedZurueck(string url)
    {
        var response = await _client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
