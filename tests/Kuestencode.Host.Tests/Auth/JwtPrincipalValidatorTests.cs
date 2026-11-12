using System.Security.Claims;
using FluentAssertions;
using Kuestencode.Shared.UI.Auth;
using Kuestencode.Werkbank.Host.Models;
using Kuestencode.Werkbank.Host.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using UserRole = Kuestencode.Shared.Contracts.Host.UserRole;

namespace Kuestencode.Host.Tests.Auth;

/// <summary>
/// Prüft das Zusammenspiel Host (Ausstellung) → Modul (Validierung), Security-Audit K2/H1.
/// </summary>
public class JwtPrincipalValidatorTests
{
    private const string Secret = "validator-tests-secret-0123456789abcdef";

    private static IConfiguration Config(string? secret, int? expiresInDays = null)
    {
        var values = new Dictionary<string, string?> { ["Jwt:Secret"] = secret };
        if (expiresInDays.HasValue)
            values["Jwt:ExpiresInDays"] = expiresInDays.Value.ToString();
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static JwtTokenService Host(string secret = Secret, int? expiresInDays = null) =>
        new(Config(secret, expiresInDays), NullLogger<JwtTokenService>.Instance);

    private static TeamMember Member() => new()
    {
        Id = Guid.NewGuid(),
        DisplayName = "Max Mustermann",
        Email = "max@example.com",
        Role = UserRole.Buero
    };

    [Fact]
    public void Validate_VomHostAusgestelltesSessionToken_LiefertPrincipalMitRolleUndId()
    {
        var member = Member();
        var token = Host().GenerateToken(member);

        var principal = new JwtPrincipalValidator(Config(Secret)).Validate(token);

        principal.Should().NotBeNull();
        principal!.FindFirstValue(ClaimTypes.NameIdentifier).Should().Be(member.Id.ToString());
        principal!.FindFirstValue(ClaimTypes.Role).Should().Be("Buero");
        principal!.Identity!.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void Validate_TokenMitFremdemSecret_LiefertNull()
    {
        var token = Host(secret: "attacker-chosen-secret-0123456789abcdef").GenerateToken(Member());

        new JwtPrincipalValidator(Config(Secret)).Validate(token).Should().BeNull();
    }

    [Fact]
    public void Validate_MfaZwischenToken_LiefertNull()
    {
        var mfaToken = Host().GenerateMfaToken(Member());

        new JwtPrincipalValidator(Config(Secret)).Validate(mfaToken).Should().BeNull();
    }

    [Fact]
    public void Validate_AbgelaufenesToken_LiefertNull()
    {
        var token = Host(expiresInDays: -1).GenerateToken(Member());

        new JwtPrincipalValidator(Config(Secret)).Validate(token).Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("kein-jwt")]
    [InlineData("eyJhbGciOiJub25lIn0.eyJyb2xlIjoiQWRtaW4ifQ.")]
    public void Validate_UngueltigeEingabe_LiefertNull(string token)
    {
        new JwtPrincipalValidator(Config(Secret)).Validate(token).Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("zu-kurz")]
    public void Konstruktor_OhneAusreichendesSecret_WirftMitHinweisAufJwtSecret(string? secret)
    {
        var act = () => new JwtPrincipalValidator(Config(secret));

        act.Should().Throw<InvalidOperationException>().WithMessage("*Jwt__Secret*");
    }

    [Fact]
    public void ExtractToken_CookieVorAuthorizationHeader()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Append("Cookie", $"{WerkbankJwt.CookieName}=aus-cookie");
        context.Request.Headers.Authorization = "Bearer aus-header";

        WerkbankJwt.ExtractToken(context).Should().Be("aus-cookie");
    }

    [Fact]
    public void ExtractToken_NurBearerHeader_LiefertHeaderToken()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer aus-header";

        WerkbankJwt.ExtractToken(context).Should().Be("aus-header");
    }

    [Fact]
    public void ExtractToken_OhneToken_LiefertNull()
    {
        WerkbankJwt.ExtractToken(new DefaultHttpContext()).Should().BeNull();
    }
}
