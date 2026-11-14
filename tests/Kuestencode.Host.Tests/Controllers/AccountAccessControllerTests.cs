using System.Security.Claims;
using FluentAssertions;
using Kuestencode.Core.Interfaces;
using Kuestencode.Werkbank.Host.Controllers;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Models;
using Kuestencode.Werkbank.Host.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using UserRole = Kuestencode.Shared.Contracts.Host.UserRole;

namespace Kuestencode.Host.Tests.Controllers;

/// <summary>
/// Security-Audit N1 und H1/M2: Rollenprüfung an Einzel-Endpunkten und Fehlversuchszählung beim zweiten Faktor.
/// </summary>
public sealed class AccountAccessControllerTests : IDisposable
{
    private readonly HostDbContext _db = new(new DbContextOptionsBuilder<HostDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    public void Dispose() => _db.Dispose();

    private static ControllerContext As(Guid id, string role, string authType = "jwt") => new()
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, id.ToString()), new Claim(ClaimTypes.Role, role)], authType))
        }
    };

    private TeamMembersController TeamMembers(TeamMember member, ControllerContext context)
    {
        var service = new Mock<ITeamMemberService>();
        service.Setup(s => s.GetByIdAsync(member.Id)).ReturnsAsync(member);
        return new TeamMembersController(service.Object, Mock.Of<IInviteService>()) { ControllerContext = context };
    }

    private static readonly TeamMember Erika = new() { Id = Guid.NewGuid(), DisplayName = "Erika", Kostensatz = 55m };

    [Fact]
    public async Task GetTeamMember_MitarbeiterFremderDatensatz_Gibt403()
    {
        var result = await TeamMembers(Erika, As(Guid.NewGuid(), "Mitarbeiter")).GetTeamMemberAsync(Erika.Id);

        result.Result.Should().BeOfType<StatusCodeResult>().Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task GetTeamMember_MitarbeiterEigenerDatensatz_IstErlaubt()
    {
        var result = await TeamMembers(Erika, As(Erika.Id, "Mitarbeiter")).GetTeamMemberAsync(Erika.Id);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Theory]
    [InlineData("Buero")]
    [InlineData("Admin")]
    public async Task GetTeamMember_BueroUndAdmin_SehenAlle(string role)
    {
        var result = await TeamMembers(Erika, As(Guid.NewGuid(), role)).GetTeamMemberAsync(Erika.Id);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void Navigation_Anonym_Gibt401()
    {
        var controller = new NavigationController(Mock.Of<IHostNavigationService>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        controller.GetNavigationItems().Result.Should().BeOfType<UnauthorizedResult>();
    }

    private AuthController Auth(Mock<ITotpService> totp, IAuthService authService)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Secret"] = new string('m', 32) })
            .Build();
        var jwt = new JwtTokenService(configuration, NullLogger<JwtTokenService>.Instance);
        return new AuthController(Mock.Of<IPasswordResetService>(), authService, totp.Object, new PasswordService(),
            _db, configuration, jwt)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    private (TeamMember Member, string MfaToken, AuthService AuthService) MfaMember()
    {
        var member = new TeamMember
        {
            Id = Guid.NewGuid(), DisplayName = "Max", Email = "m@x.de", Role = UserRole.Admin,
            MfaEnabled = true, TotpSecret = "secret", InviteAcceptedAt = DateTime.UtcNow
        };
        _db.TeamMembers.Add(member);
        _db.SaveChanges();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Secret"] = new string('m', 32) })
            .Build();
        var jwt = new JwtTokenService(configuration, NullLogger<JwtTokenService>.Instance);
        var authService = new AuthService(_db, new PasswordService(), jwt, NullLogger<AuthService>.Instance);
        return (member, jwt.GenerateMfaToken(member), authService);
    }

    [Fact]
    public async Task LoginMfa_DreiFalscheCodes_SperrtKontoUndLehntDanachAuchRichtigenCodeAb()
    {
        var (member, mfaToken, authService) = MfaMember();
        var totp = new Mock<ITotpService>();
        totp.Setup(t => t.VerifyCode("secret", "000000")).Returns(false);
        totp.Setup(t => t.VerifyCode("secret", "123456")).Returns(true);
        var controller = Auth(totp, authService);

        for (var i = 0; i < 3; i++)
            await controller.LoginMfa(new MfaLoginRequest { MfaToken = mfaToken, Code = "000000" });
        var afterLockout = await controller.LoginMfa(new MfaLoginRequest { MfaToken = mfaToken, Code = "123456" });

        member.FailedLoginAttempts.Should().Be(3);
        member.LockoutUntil.Should().BeAfter(DateTime.UtcNow);
        afterLockout.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task LoginMfa_RichtigerCode_SetztFehlversucheZurueck()
    {
        var (member, mfaToken, authService) = MfaMember();
        var totp = new Mock<ITotpService>();
        totp.Setup(t => t.VerifyCode("secret", "000000")).Returns(false);
        totp.Setup(t => t.VerifyCode("secret", "123456")).Returns(true);
        var controller = Auth(totp, authService);

        await controller.LoginMfa(new MfaLoginRequest { MfaToken = mfaToken, Code = "000000" });
        var result = await controller.LoginMfa(new MfaLoginRequest { MfaToken = mfaToken, Code = "123456" });

        result.Should().BeOfType<OkObjectResult>();
        member.FailedLoginAttempts.Should().Be(0);
    }

    [Fact]
    public async Task LoginMfa_SessionTokenStattMfaToken_Gibt401()
    {
        var (member, _, authService) = MfaMember();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Secret"] = new string('m', 32) })
            .Build();
        var sessionToken = new JwtTokenService(configuration, NullLogger<JwtTokenService>.Instance).GenerateToken(member);

        var result = await Auth(new Mock<ITotpService>(), authService)
            .LoginMfa(new MfaLoginRequest { MfaToken = sessionToken, Code = "123456" });

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }
}
