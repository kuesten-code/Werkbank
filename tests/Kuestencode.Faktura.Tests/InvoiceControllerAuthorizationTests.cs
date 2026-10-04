using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Kuestencode.Faktura.Tests.TestDoubles;
using Xunit;

namespace Kuestencode.Faktura.Tests;

/// <summary>
/// Regressionstest für die im September 2026 gefundene Autorisierungslücke: Modul-Controller
/// müssen unauthentifizierte Requests ablehnen, unabhängig davon, ob sie direkt oder über den
/// Host-Reverse-Proxy erreicht werden. Ohne diesen Test käme die Lücke beim nächsten
/// Middleware-Umbau unbemerkt zurück, da alle anderen Tests bewusst einen gültigen Test-JWT
/// mitschicken (siehe TestDoubles.TestJwt).
/// </summary>
public class InvoiceControllerAuthorizationTests : IClassFixture<FakturaWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FakturaWebApplicationFactory _factory;

    public InvoiceControllerAuthorizationTests(FakturaWebApplicationFactory factory)
    {
        _factory = factory;
        // Bewusst OHNE Authorization-Header — im Gegensatz zu InvoiceControllerTests.
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_OhneToken_GibtUnauthorizedZurueck()
    {
        var response = await _client.GetAsync("/api/Invoice");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_OhneToken_GibtUnauthorizedZurueck()
    {
        var response = await _client.PostAsJsonAsync("/api/Invoice", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Die andere Hälfte von [RequireRole]: ein strukturell gültiges Token mit einer Rolle,
    /// die für den InvoiceController nicht zugelassen ist (Admin/Buero, nicht Mitarbeiter),
    /// muss mit 403 abgelehnt werden statt mit 401 (kein Token) oder 200 (durchgewunken).
    /// </summary>
    [Fact]
    public async Task GetAll_GueltigesTokenAberFalscheRolle_GibtForbiddenZurueck()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.Create("Mitarbeiter"));

        var response = await client.GetAsync("/api/Invoice");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
