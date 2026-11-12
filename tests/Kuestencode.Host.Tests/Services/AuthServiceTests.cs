using FluentAssertions;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Models;
using Kuestencode.Werkbank.Host.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using UserRole = Kuestencode.Shared.Contracts.Host.UserRole;

namespace Kuestencode.Host.Tests.Services;

public class AuthServiceTests : IDisposable
{
    private const string Email = "max@example.com";
    private const string Password = "Richtig123!";

    private readonly HostDbContext _context;
    private readonly JwtTokenService _jwtTokenService;
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _context = new HostDbContext(new DbContextOptionsBuilder<HostDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Secret"] = new string('s', 32) })
            .Build();
        _jwtTokenService = new JwtTokenService(configuration, NullLogger<JwtTokenService>.Instance);

        var passwordService = new PasswordService();
        _service = new AuthService(_context, passwordService, _jwtTokenService, NullLogger<AuthService>.Instance);
    }

    public void Dispose() => _context.Dispose();

    private TeamMember AddMember(bool mfaEnabled = false)
    {
        var member = new TeamMember
        {
            Id = Guid.NewGuid(),
            DisplayName = "Max Mustermann",
            Email = Email,
            Role = UserRole.Buero,
            PasswordHash = new PasswordService().HashPassword(Password),
            InviteAcceptedAt = DateTime.UtcNow,
            MfaEnabled = mfaEnabled
        };
        _context.TeamMembers.Add(member);
        _context.SaveChanges();
        return member;
    }

    [Fact]
    public async Task LoginAsync_OhneMfa_LiefertGueltigesSessionToken()
    {
        var member = AddMember();

        var result = await _service.LoginAsync(Email, Password);

        result.Success.Should().BeTrue();
        result.RequiresMfa.Should().BeFalse();
        result.User!.Id.Should().Be(member.Id);
        _jwtTokenService.ValidateToken(result.Token!).Should().NotBeNull();
    }

    /// <summary>
    /// Regressionstest Security-Audit H1: nach Schritt 1 gibt es nur einen MFA-Token,
    /// der für die Session-Validierung ungültig ist.
    /// </summary>
    [Fact]
    public async Task LoginAsync_MitMfa_LiefertNurMfaTokenDasKeineSessionIst()
    {
        AddMember(mfaEnabled: true);

        var result = await _service.LoginAsync(Email, Password);

        result.Success.Should().BeTrue();
        result.RequiresMfa.Should().BeTrue();
        result.Token.Should().BeNull();
        _jwtTokenService.ValidateMfaToken(result.MfaToken!).Should().NotBeNull();
        _jwtTokenService.ValidateToken(result.MfaToken!).Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_UnbekannteEmail_SchlaegtFehl()
    {
        var result = await _service.LoginAsync("niemand@example.com", Password);

        result.Success.Should().BeFalse();
        result.Token.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_FalschesPasswort_SchlaegtFehlUndZaehltFehlversuch()
    {
        var member = AddMember();

        var result = await _service.LoginAsync(Email, "falsch");

        result.Success.Should().BeFalse();
        result.RemainingAttempts.Should().Be(2);
        (await _context.TeamMembers.FindAsync(member.Id))!.FailedLoginAttempts.Should().Be(1);
    }

    [Fact]
    public async Task GetCurrentUserAsync_GueltigeId_LiefertAktivesMitglied()
    {
        var member = AddMember();

        var result = await _service.GetCurrentUserAsync(member.Id.ToString());

        result!.Id.Should().Be(member.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("keine-guid")]
    public async Task GetCurrentUserAsync_UngueltigeId_LiefertNull(string? userId)
    {
        (await _service.GetCurrentUserAsync(userId)).Should().BeNull();
    }
}
