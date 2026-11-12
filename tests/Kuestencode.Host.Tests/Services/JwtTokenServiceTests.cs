using System.Security.Claims;
using FluentAssertions;
using Kuestencode.Werkbank.Host.Models;
using Kuestencode.Werkbank.Host.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using UserRole = Kuestencode.Shared.Contracts.Host.UserRole;

namespace Kuestencode.Host.Tests.Services;

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService(string secret)
    {
        var values = new Dictionary<string, string?> { ["Jwt:Issuer"] = "TestIssuer", ["Jwt:Secret"] = secret };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        return new JwtTokenService(configuration, NullLogger<JwtTokenService>.Instance);
    }

    private static TeamMember CreateMember() => new()
    {
        Id = Guid.NewGuid(),
        DisplayName = "Max Mustermann",
        Email = "max@example.com",
        Role = UserRole.Admin
    };

    [Fact]
    public void ValidateToken_MitEigenemGueltigenToken_LiefertPrincipalMitClaims()
    {
        var service = CreateService(secret: new string('a', 32));
        var member = CreateMember();

        var token = service.GenerateToken(member);
        var principal = service.ValidateToken(token);

        principal.Should().NotBeNull();
        principal!.FindFirstValue(ClaimTypes.NameIdentifier).Should().Be(member.Id.ToString());
        principal!.FindFirstValue(ClaimTypes.Role).Should().Be(UserRole.Admin.ToString());
    }

    [Fact]
    public void ValidateToken_TokenWurdeMitAnderemSecretSigniert_LiefertNull()
    {
        // Simuliert eine Secret-Rotation: ein Token, das mit dem alten Secret ausgestellt wurde,
        // darf nach einem Secret-Wechsel nicht mehr als gueltig durchgehen.
        var issuingService = CreateService(secret: new string('a', 32));
        var validatingService = CreateService(secret: new string('b', 32));

        var token = issuingService.GenerateToken(CreateMember());
        var principal = validatingService.ValidateToken(token);

        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_AbgelaufenerToken_LiefertNull()
    {
        var values = new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "TestIssuer",
            ["Jwt:Secret"] = new string('a', 32),
            ["Jwt:ExpiresInDays"] = "-1"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var service = new JwtTokenService(configuration, NullLogger<JwtTokenService>.Instance);

        var token = service.GenerateToken(CreateMember());
        var principal = service.ValidateToken(token);

        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_UngueltigerTokenString_LiefertNullStattException()
    {
        var service = CreateService(secret: new string('a', 32));

        var principal = service.ValidateToken("das-ist-kein-jwt");

        principal.Should().BeNull();
    }

    /// <summary>
    /// Regressionstest Security-Audit H1: der MFA-Zwischen-Token darf nicht als Session gelten.
    /// </summary>
    [Fact]
    public void ValidateToken_MfaZwischenToken_LiefertNull()
    {
        var service = CreateService(secret: new string('a', 32));

        var mfaToken = service.GenerateMfaToken(CreateMember());

        service.ValidateToken(mfaToken).Should().BeNull();
    }

    [Fact]
    public void ValidateMfaToken_MfaZwischenToken_LiefertPrincipalMitMemberIdOhneRolle()
    {
        var service = CreateService(secret: new string('a', 32));
        var member = CreateMember();

        var principal = service.ValidateMfaToken(service.GenerateMfaToken(member));

        principal.Should().NotBeNull();
        principal!.FindFirstValue(ClaimTypes.NameIdentifier).Should().Be(member.Id.ToString());
        principal!.FindFirstValue(ClaimTypes.Role).Should().BeNull();
    }

    [Fact]
    public void ValidateMfaToken_SessionToken_LiefertNull()
    {
        var service = CreateService(secret: new string('a', 32));

        var sessionToken = service.GenerateToken(CreateMember());

        service.ValidateMfaToken(sessionToken).Should().BeNull();
    }
}
