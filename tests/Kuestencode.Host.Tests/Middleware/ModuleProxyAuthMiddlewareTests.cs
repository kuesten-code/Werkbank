using System.Security.Claims;
using FluentAssertions;
using Kuestencode.Shared.UI.Auth;
using Kuestencode.Werkbank.Host.Middleware;
using Kuestencode.Werkbank.Host.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Kuestencode.Host.Tests.Middleware;

public class ModuleProxyAuthMiddlewareTests
{
    private const string Secret = "proxy-tests-secret-0123456789abcdefgh";

    private static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Secret"] = Secret })
        .Build();

    private static readonly JwtTokenService JwtService = new(Configuration, NullLogger<JwtTokenService>.Instance);

    private static async Task<string?> ForwardedAuthorization(HttpContext context)
    {
        var nextCalled = false;
        await new ModuleProxyAuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; })
            .InvokeAsync(context, JwtService);
        nextCalled.Should().BeTrue();
        return context.Request.Headers.Authorization.FirstOrDefault();
    }

    /// <summary>
    /// K1-Modulschicht: auch bei deaktivierter Anmeldung bekommt das Modul ein signiertes Admin-Token.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_AnmeldungDeaktiviert_GibtModulGueltigesAdminTokenMit()
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(Array.Empty<Claim>(), AuthMiddleware.AuthDisabledType))
        };
        context.Request.Headers.Append("Cookie", $"{WerkbankJwt.CookieName}=altes-ungueltiges-cookie");

        var header = await ForwardedAuthorization(context);

        var principal = new JwtPrincipalValidator(Configuration).Validate(header!["Bearer ".Length..]);
        principal!.FindFirstValue(ClaimTypes.Role).Should().Be("Admin");
    }

    [Fact]
    public async Task InvokeAsync_AngemeldetPerCookie_SetztCookieTokenAlsBearer()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Append("Cookie", $"{WerkbankJwt.CookieName}=session-token");

        (await ForwardedAuthorization(context)).Should().Be("Bearer session-token");
    }

    [Fact]
    public async Task InvokeAsync_OhneToken_SetztKeinenHeader()
    {
        (await ForwardedAuthorization(new DefaultHttpContext())).Should().BeNull();
    }
}
