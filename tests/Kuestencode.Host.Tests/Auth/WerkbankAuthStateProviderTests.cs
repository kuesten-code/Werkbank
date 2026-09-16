using FluentAssertions;
using Kuestencode.Werkbank.Host.Auth;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Models;
using Kuestencode.Werkbank.Host.Services;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Moq;
using Xunit;
using UserRole = Kuestencode.Shared.Contracts.Host.UserRole;

namespace Kuestencode.Host.Tests.Auth;

public class WerkbankAuthStateProviderTests : IDisposable
{
    private readonly ServiceProvider _rootProvider;

    public WerkbankAuthStateProviderTests()
    {
        // AddDbContext(o => o.UseInMemoryDatabase(name)) reicht hier nicht: die Konfigurations-
        // Lambda erzeugt bei jedem CreateScope() intern eine neue InMemoryDatabaseRoot, wodurch
        // separate Scopes trotz gleichen Namens isolierte Stores sehen. Deshalb eine einzige
        // DbContextOptions-Instanz für alle Scopes dieses Tests wiederverwenden.
        var options = new DbContextOptionsBuilder<HostDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var services = new ServiceCollection();
        services.AddScoped(_ => new HostDbContext(options));
        _rootProvider = services.BuildServiceProvider();

        using var scope = _rootProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HostDbContext>();
        db.WerkbankSettings.Add(new WerkbankSettings { Id = Guid.NewGuid(), AuthEnabled = true });
        db.SaveChanges();
    }

    public void Dispose() => _rootProvider.Dispose();

    private static JwtTokenService CreateJwtService(string secret)
    {
        var values = new Dictionary<string, string?> { ["Jwt:Issuer"] = "TestIssuer", ["Jwt:Secret"] = secret };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        return new JwtTokenService(configuration, NullLogger<JwtTokenService>.Instance);
    }

    private WerkbankAuthStateProvider CreateProvider(IJwtTokenService jwtTokenService, string? cookieToken)
    {
        var httpContext = new DefaultHttpContext();
        if (cookieToken != null)
            httpContext.Request.Headers.Append("Cookie", $"werkbank_auth_cookie={cookieToken}");

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext);

        var localStorage = new ProtectedLocalStorage(Mock.Of<IJSRuntime>(), new EphemeralDataProtectionProvider());

        return new WerkbankAuthStateProvider(
            localStorage,
            httpContextAccessor.Object,
            _rootProvider,
            jwtTokenService,
            NullLogger<WerkbankAuthStateProvider>.Instance);
    }

    private static TeamMember CreateMember() => new()
    {
        Id = Guid.NewGuid(),
        DisplayName = "Max Mustermann",
        Role = UserRole.Admin
    };

    [Fact]
    public async Task GetAuthenticationStateAsync_GueltigesCookieToken_IstAuthentifiziert()
    {
        var jwtService = CreateJwtService(new string('a', 32));
        var member = CreateMember();
        var token = jwtService.GenerateToken(member);
        using var provider = CreateProvider(jwtService, token);

        var state = await provider.GetAuthenticationStateAsync();

        state.User.Identity!.IsAuthenticated.Should().BeTrue();
        state.User.Identity!.AuthenticationType.Should().Be("jwt");
        state.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value.Should().Be(member.Id.ToString());
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_TokenMitFalscherSignatur_IstAnonymTrotzGueltigemAblauf()
    {
        // Regressionstest fuer den Login-Enforcement-Bug: die alte Implementierung
        // (JwtSecurityTokenHandler.ReadJwtToken) prüfte nur das Ablaufdatum, nicht die
        // Signatur - ein Token, das nach einer Secret-Rotation nicht mehr passt, aber noch
        // nicht abgelaufen ist, wurde faelschlich als eingeloggt behandelt.
        var issuingService = CreateJwtService(new string('a', 32));
        var validatingService = CreateJwtService(new string('b', 32));
        var token = issuingService.GenerateToken(CreateMember());

        using var provider = CreateProvider(validatingService, token);

        var state = await provider.GetAuthenticationStateAsync();

        state.User.Identity!.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_OhneCookie_IstAnonym()
    {
        var jwtService = CreateJwtService(new string('a', 32));
        using var provider = CreateProvider(jwtService, cookieToken: null);

        var state = await provider.GetAuthenticationStateAsync();

        state.User.Identity!.IsAuthenticated.Should().BeFalse();
    }
}
