using System.Security.Claims;
using FluentAssertions;
using Kuestencode.Shared.UI.Auth;
using Kuestencode.Werkbank.Host.Models;
using Kuestencode.Werkbank.Host.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Moq;
using Xunit;
using UserRole = Kuestencode.Shared.Contracts.Host.UserRole;

namespace Kuestencode.Host.Tests.Auth;

/// <summary>
/// Blazor-Identität der Module (Security-Audit K2): nur vom Host signierte Tokens zählen,
/// egal ob sie per Cookie (Prerender) oder per JS-Interop (Circuit) ankommen.
/// </summary>
public class PassThroughAuthStateProviderTests
{
    private const string Secret = "passthrough-tests-secret-0123456789abcdef";

    private static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Secret"] = Secret })
        .Build();

    private static string HostToken(string secret = Secret)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Secret"] = secret })
            .Build();
        var member = new TeamMember { Id = Guid.NewGuid(), DisplayName = "Erika", Role = UserRole.Admin };
        return new JwtTokenService(config, NullLogger<JwtTokenService>.Instance).GenerateToken(member);
    }

    private static PassThroughAuthStateProvider CreateProvider(HttpContext? httpContext, string? jsToken = null)
    {
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var js = new Mock<IJSRuntime>();
        js.Setup(r => r.InvokeAsync<string?>("getWerkbankJwt", It.IsAny<object?[]>()))
            .ReturnsAsync(jsToken);

        return new PassThroughAuthStateProvider(accessor, js.Object,
            NullLogger<PassThroughAuthStateProvider>.Instance, new JwtPrincipalValidator(Configuration));
    }

    private static HttpContext ContextWithCookie(string token)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Append("Cookie", $"{WerkbankJwt.CookieName}={token}");
        return context;
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_GueltigesCookie_LiefertAdmin()
    {
        var provider = CreateProvider(ContextWithCookie(HostToken()));

        var state = await provider.GetAuthenticationStateAsync();

        state.User.FindFirstValue(ClaimTypes.Role).Should().Be("Admin");
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_GefaelschtesCookie_LiefertAnonym()
    {
        var provider = CreateProvider(ContextWithCookie(HostToken(secret: "attacker-chosen-secret-0123456789abcdef")));

        var state = await provider.GetAuthenticationStateAsync();

        state.User.Identity!.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_GueltigesTokenPerJsInterop_LiefertAdmin()
    {
        var provider = CreateProvider(httpContext: null, jsToken: HostToken());

        var state = await provider.GetAuthenticationStateAsync();

        state.User.FindFirstValue(ClaimTypes.Role).Should().Be("Admin");
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_GefaelschtesTokenPerJsInterop_LiefertAnonym()
    {
        var provider = CreateProvider(httpContext: null, jsToken: HostToken(secret: "attacker-chosen-secret-0123456789abcdef"));

        var state = await provider.GetAuthenticationStateAsync();

        state.User.Identity!.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_ZweiterAufruf_NutztAufgeloestenZustand()
    {
        var provider = CreateProvider(ContextWithCookie(HostToken()));

        var first = await provider.GetAuthenticationStateAsync();
        var second = await provider.GetAuthenticationStateAsync();

        second.Should().BeSameAs(first);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_HttpContextUserBereitsAuthentifiziert_UebernimmtIhn()
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Buero") }, "jwt"))
        };
        var provider = CreateProvider(context);

        var state = await provider.GetAuthenticationStateAsync();

        state.User.FindFirstValue(ClaimTypes.Role).Should().Be("Buero");
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_JsInteropNichtVerfuegbar_LiefertAnonym()
    {
        var accessor = new HttpContextAccessor();
        var js = new Mock<IJSRuntime>();
        js.Setup(r => r.InvokeAsync<string?>("getWerkbankJwt", It.IsAny<object?[]>()))
            .ThrowsAsync(new InvalidOperationException("prerender"));
        var provider = new PassThroughAuthStateProvider(accessor, js.Object,
            NullLogger<PassThroughAuthStateProvider>.Instance, new JwtPrincipalValidator(Configuration));

        var state = await provider.GetAuthenticationStateAsync();

        state.User.Identity!.IsAuthenticated.Should().BeFalse();
    }
}
