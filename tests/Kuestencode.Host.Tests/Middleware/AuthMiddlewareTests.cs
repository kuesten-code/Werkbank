using FluentAssertions;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Middleware;
using Kuestencode.Werkbank.Host.Models;
using Kuestencode.Werkbank.Host.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Model;
using Xunit;
using UserRole = Kuestencode.Shared.Contracts.Host.UserRole;

namespace Kuestencode.Host.Tests.Middleware;

public class AuthMiddlewareTests
{
    private static JwtTokenService CreateJwtService(string secret)
    {
        var values = new Dictionary<string, string?> { ["Jwt:Issuer"] = "TestIssuer", ["Jwt:Secret"] = secret };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        return new JwtTokenService(configuration, NullLogger<JwtTokenService>.Instance);
    }

    private static DefaultHttpContext CreateHttpContext(string path, bool authEnabled, string? cookieToken = null)
    {
        // Gleiche DbContextOptions-Instanz für alle Scopes: AddDbContext(o => UseInMemoryDatabase(name))
        // würde pro Scope eine neue InMemoryDatabaseRoot erzeugen und die gesetzten Settings verlieren.
        var options = new DbContextOptionsBuilder<HostDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using (var seedContext = new HostDbContext(options))
        {
            seedContext.WerkbankSettings.Add(new WerkbankSettings { Id = Guid.NewGuid(), AuthEnabled = authEnabled });
            seedContext.SaveChanges();
        }

        var services = new ServiceCollection();
        services.AddScoped(_ => new HostDbContext(options));

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
        httpContext.Request.Path = path;
        if (cookieToken != null)
            httpContext.Request.Headers.Append("Cookie", $"werkbank_auth_cookie={cookieToken}");

        return httpContext;
    }

    /// <summary>
    /// Simuliert, dass das Routing den Request einer YARP-Modulroute zugeordnet hat.
    /// </summary>
    private static void MarkAsModuleRoute(HttpContext httpContext)
    {
        var route = new RouteModel(new RouteConfig { RouteId = "faktura-route" }, cluster: null, HttpTransformer.Empty);
        httpContext.SetEndpoint(new Endpoint(null, new EndpointMetadataCollection(route), "faktura-route"));
    }

    private static (AuthMiddleware Middleware, Func<bool> NextCalled) CreateMiddleware(JwtTokenService? jwtService = null)
    {
        var nextCalled = false;
        var middleware = new AuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; },
            jwtService ?? CreateJwtService(new string('a', 32)), NullLogger<AuthMiddleware>.Instance);
        return (middleware, () => nextCalled);
    }

    private static TeamMember CreateMember() => new()
    {
        Id = Guid.NewGuid(),
        DisplayName = "Max Mustermann",
        Role = UserRole.Mitarbeiter
    };

    [Fact]
    public async Task InvokeAsync_AuthDeaktiviert_SetztImplizitenAdminUndRuftNextAuf()
    {
        var httpContext = CreateHttpContext("/api/customers", authEnabled: false);
        var nextCalled = false;
        var middleware = new AuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; },
            CreateJwtService(new string('a', 32)), NullLogger<AuthMiddleware>.Instance);

        await middleware.InvokeAsync(httpContext);

        nextCalled.Should().BeTrue();
        httpContext.User.Identity!.AuthenticationType.Should().Be("NoAuth");
        httpContext.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_ApiPfadOhneToken_Gibt401ZurueckOhneNextAufzurufen()
    {
        var httpContext = CreateHttpContext("/api/customers", authEnabled: true);
        var nextCalled = false;
        var middleware = new AuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; },
            CreateJwtService(new string('a', 32)), NullLogger<AuthMiddleware>.Instance);

        await middleware.InvokeAsync(httpContext);

        nextCalled.Should().BeFalse();
        httpContext.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task InvokeAsync_SeitenaufrufOhneToken_LaesstDurchFuerAuthStateProviderRedirect()
    {
        var httpContext = CreateHttpContext("/customers", authEnabled: true);
        var nextCalled = false;
        var middleware = new AuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; },
            CreateJwtService(new string('a', 32)), NullLogger<AuthMiddleware>.Instance);

        await middleware.InvokeAsync(httpContext);

        nextCalled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_GueltigesToken_SetztUserUndRuftNextAuf()
    {
        var jwtService = CreateJwtService(new string('a', 32));
        var member = CreateMember();
        var token = jwtService.GenerateToken(member);
        var httpContext = CreateHttpContext("/api/customers", authEnabled: true, cookieToken: token);
        var nextCalled = false;
        var middleware = new AuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; },
            jwtService, NullLogger<AuthMiddleware>.Instance);

        await middleware.InvokeAsync(httpContext);

        nextCalled.Should().BeTrue();
        httpContext.User.Identity!.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_TokenMitFalscherSignatur_Gibt401ZurueckTrotzGueltigemAblauf()
    {
        // Regressionstest: nach einer Secret-Rotation ausgestellte Tokens duerfen die
        // Middleware nicht mehr passieren, obwohl sie noch nicht abgelaufen sind.
        var issuingService = CreateJwtService(new string('a', 32));
        var validatingService = CreateJwtService(new string('b', 32));
        var token = issuingService.GenerateToken(CreateMember());
        var httpContext = CreateHttpContext("/api/customers", authEnabled: true, cookieToken: token);
        var nextCalled = false;
        var middleware = new AuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; },
            validatingService, NullLogger<AuthMiddleware>.Instance);

        await middleware.InvokeAsync(httpContext);

        nextCalled.Should().BeFalse();
        httpContext.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task InvokeAsync_ModulSeiteOhneToken_LeitetZumLoginMitReturnUrlUm()
    {
        var httpContext = CreateHttpContext("/faktura/invoices", authEnabled: true);
        httpContext.Request.QueryString = new QueryString("?status=open");
        MarkAsModuleRoute(httpContext);
        var (middleware, nextCalled) = CreateMiddleware();

        await middleware.InvokeAsync(httpContext);

        nextCalled().Should().BeFalse();
        httpContext.Response.StatusCode.Should().Be(302);
        httpContext.Response.Headers.Location.ToString()
            .Should().Be("/login?returnUrl=%2Ffaktura%2Finvoices%3Fstatus%3Dopen");
    }

    [Fact]
    public async Task InvokeAsync_ModulSeiteMitUngueltigemToken_LeitetZumLoginUm()
    {
        var httpContext = CreateHttpContext("/faktura/invoices", authEnabled: true, cookieToken: "kaputt");
        MarkAsModuleRoute(httpContext);
        var (middleware, nextCalled) = CreateMiddleware();

        await middleware.InvokeAsync(httpContext);

        nextCalled().Should().BeFalse();
        httpContext.Response.StatusCode.Should().Be(302);
    }

    [Theory]
    [InlineData("/faktura/_blazor")]
    [InlineData("/_faktura/_framework/blazor.server.js")]
    [InlineData("/faktura/css/app.css")]
    public async Task InvokeAsync_ModulPfadDerFuerDenHostOeffentlichWaere_WirdOhneTokenNichtDurchgereicht(string path)
    {
        var httpContext = CreateHttpContext(path, authEnabled: true);
        MarkAsModuleRoute(httpContext);
        var (middleware, nextCalled) = CreateMiddleware();

        await middleware.InvokeAsync(httpContext);

        nextCalled().Should().BeFalse();
        httpContext.Response.StatusCode.Should().Be(302);
    }

    [Fact]
    public async Task InvokeAsync_ModulApiMitPunktImPfadOhneToken_Gibt401()
    {
        var httpContext = CreateHttpContext("/recepta/api/recepta/files/beleg.pdf", authEnabled: true);
        MarkAsModuleRoute(httpContext);
        var (middleware, nextCalled) = CreateMiddleware();

        await middleware.InvokeAsync(httpContext);

        nextCalled().Should().BeFalse();
        httpContext.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task InvokeAsync_ModulSeiteMitGueltigemToken_ReichtDurch()
    {
        var jwtService = CreateJwtService(new string('a', 32));
        var token = jwtService.GenerateToken(CreateMember());
        var httpContext = CreateHttpContext("/faktura/invoices", authEnabled: true, cookieToken: token);
        MarkAsModuleRoute(httpContext);
        var (middleware, nextCalled) = CreateMiddleware(jwtService);

        await middleware.InvokeAsync(httpContext);

        nextCalled().Should().BeTrue();
        httpContext.User.Identity!.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_ModulSeiteBeiDeaktivierterAuth_ReichtDurch()
    {
        var httpContext = CreateHttpContext("/faktura/invoices", authEnabled: false);
        MarkAsModuleRoute(httpContext);
        var (middleware, nextCalled) = CreateMiddleware();

        await middleware.InvokeAsync(httpContext);

        nextCalled().Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_HostApiMitPunktImPfadOhneToken_Gibt401()
    {
        var httpContext = CreateHttpContext("/api/company/logo.png", authEnabled: true);
        var (middleware, nextCalled) = CreateMiddleware();

        await middleware.InvokeAsync(httpContext);

        nextCalled().Should().BeFalse();
        httpContext.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task InvokeAsync_StatischeHostDateiOhneToken_ReichtDurch()
    {
        var httpContext = CreateHttpContext("/favicon.png", authEnabled: true);
        var (middleware, nextCalled) = CreateMiddleware();

        await middleware.InvokeAsync(httpContext);

        nextCalled().Should().BeTrue();
    }

    /// <summary>
    /// Regressionstest Security-Audit H1: der MFA-Zwischen-Token ersetzt keine Session.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_MfaZwischenTokenAlsCookie_Gibt401()
    {
        var jwtService = CreateJwtService(new string('a', 32));
        var mfaToken = jwtService.GenerateMfaToken(CreateMember());
        var httpContext = CreateHttpContext("/api/auth/me", authEnabled: true, cookieToken: mfaToken);
        var (middleware, nextCalled) = CreateMiddleware(jwtService);

        await middleware.InvokeAsync(httpContext);

        nextCalled().Should().BeFalse();
        httpContext.Response.StatusCode.Should().Be(401);
    }
}
