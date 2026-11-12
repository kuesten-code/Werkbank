using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Kuestencode.Rapport.Services;
using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.Contracts.Host;
using Kuestencode.Shared.UI.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace Kuestencode.Rapport.Tests.Services;

/// <summary>
/// Security-Audit K2: Rapport ermittelt Nutzer und Rolle nur aus signaturgeprüften Tokens.
/// </summary>
public class UserContextServiceTests
{
    private const string Secret = "rapport-tests-jwt-secret-0123456789abcdef";

    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

    private static string Token(Guid userId, string role, string secret = Secret)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var token = new JwtSecurityToken(
            issuer: WerkbankJwt.DefaultIssuer,
            audience: WerkbankJwt.DefaultIssuer,
            claims: new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, "Erika"),
                new Claim(ClaimTypes.Role, role)
            },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static UserContextService CreateService(string? cookieToken, Mock<IHostApiClient>? hostApi = null)
    {
        var httpContext = new DefaultHttpContext();
        if (cookieToken != null)
            httpContext.Request.Headers.Append("Cookie", $"{WerkbankJwt.CookieName}={cookieToken}");

        var authState = new Mock<AuthenticationStateProvider>();
        authState.Setup(a => a.GetAuthenticationStateAsync()).ReturnsAsync(new AuthenticationState(Anonymous));

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Secret"] = Secret })
            .Build();

        return new UserContextService(authState.Object, new HttpContextAccessor { HttpContext = httpContext },
            (hostApi ?? new Mock<IHostApiClient>()).Object, new JwtPrincipalValidator(config));
    }

    [Fact]
    public async Task GueltigesToken_LiefertIdNameUndRolle()
    {
        var userId = Guid.NewGuid();
        var service = CreateService(Token(userId, "Admin"));

        (await service.GetCurrentUserIdAsync()).Should().Be(userId);
        (await service.GetCurrentUserNameAsync()).Should().Be("Erika");
        (await service.IsAdminAsync()).Should().BeTrue();
        (await service.IsAdminOrBueroAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task GefaelschtesAdminToken_WirdIgnoriert()
    {
        var service = CreateService(Token(Guid.NewGuid(), "Admin", secret: "attacker-chosen-secret-0123456789abcdef"));

        (await service.GetCurrentUserIdAsync()).Should().BeNull();
        (await service.IsAdminAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task OhneToken_FaelltAufAuthStateProviderZurueck()
    {
        var service = CreateService(cookieToken: null);

        (await service.GetCurrentUserRoleAsync()).Should().BeNull();
        (await service.IsAdminOrBueroAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task GetCurrentUserMitarbeiterRolleAsync_LaedtRolleEinmalVomHost()
    {
        var userId = Guid.NewGuid();
        var hostApi = new Mock<IHostApiClient>();
        hostApi.Setup(h => h.GetTeamMemberAsync(userId))
            .ReturnsAsync(new TeamMemberDto { Id = userId, MitarbeiterRolleId = 3, MitarbeiterRolleName = "Geselle" });
        var service = CreateService(Token(userId, "Mitarbeiter"), hostApi);

        var first = await service.GetCurrentUserMitarbeiterRolleAsync();
        var second = await service.GetCurrentUserMitarbeiterRolleAsync();

        first.Should().Be((3, "Geselle"));
        second.Should().Be(first);
        hostApi.Verify(h => h.GetTeamMemberAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetCurrentUserMitarbeiterRolleAsync_OhneNutzer_LiefertLeer()
    {
        var service = CreateService(cookieToken: null);

        (await service.GetCurrentUserMitarbeiterRolleAsync()).Should().Be(((int?)null, (string?)null));
    }
}
