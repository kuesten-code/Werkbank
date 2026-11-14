using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Kuestencode.Shared.UI.Auth;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Models;
using Kuestencode.Werkbank.Host.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using UserRole = Kuestencode.Shared.Contracts.Host.UserRole;

namespace Kuestencode.Host.Tests.Services;

/// <summary>
/// Security-Audit M1: Deaktivieren, Sperren und Passwortwechsel beenden bestehende Sessions sofort.
/// </summary>
public sealed class SessionValidatorTests : IDisposable
{
    private static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Secret"] = new string('v', 32) })
        .Build();

    private readonly JwtTokenService _jwt = new(Configuration, NullLogger<JwtTokenService>.Instance);
    private readonly DbContextOptions<HostDbContext> _options = new DbContextOptionsBuilder<HostDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
    private readonly ServiceProvider _services;
    private readonly SessionValidator _validator;

    public SessionValidatorTests()
    {
        _services = new ServiceCollection().AddScoped(_ => new HostDbContext(_options)).BuildServiceProvider();
        _validator = new SessionValidator(_jwt, _services.GetRequiredService<IServiceScopeFactory>());
    }

    public void Dispose() => _services.Dispose();

    private TeamMember Save(Action<TeamMember>? configure = null)
    {
        var member = new TeamMember { Id = Guid.NewGuid(), DisplayName = "Erika", Email = "e@x.de", Role = UserRole.Buero };
        configure?.Invoke(member);
        using var db = new HostDbContext(_options);
        db.TeamMembers.Add(member);
        db.SaveChanges();
        return member;
    }

    private void Update(Guid id, Action<TeamMember> change)
    {
        using var db = new HostDbContext(_options);
        change(db.TeamMembers.Single(m => m.Id == id));
        db.SaveChanges();
    }

    [Fact]
    public async Task ValidateAsync_AktivesKonto_IstGueltig()
    {
        var member = Save();

        (await _validator.ValidateAsync(_jwt.GenerateToken(member))).Should().NotBeNull();
    }

    [Theory]
    [InlineData("deaktiviert")]
    [InlineData("gesperrt")]
    [InlineData("offline")]
    [InlineData("widerrufen")]
    public async Task ValidateAsync_KontoNachAusstellungGeaendert_IstUngueltig(string change)
    {
        var member = Save();
        var token = _jwt.GenerateToken(member);

        Update(member.Id, m =>
        {
            switch (change)
            {
                case "deaktiviert": m.IsActive = false; break;
                case "gesperrt": m.IsLockedByAdmin = true; break;
                case "offline": m.IsOffline = true; break;
                default: m.RevokeSessions(); break;
            }
        });

        (await _validator.ValidateAsync(token)).Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_GeloeschtesKonto_IstUngueltig()
    {
        var token = _jwt.GenerateToken(new TeamMember { Id = Guid.NewGuid(), DisplayName = "Weg", Role = UserRole.Admin });

        (await _validator.ValidateAsync(token)).Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_TokenNachWiderrufNeuAusgestellt_IstGueltig()
    {
        var member = Save(m => m.SessionVersion = 3);

        (await _validator.ValidateAsync(_jwt.GenerateToken(member))).Should().NotBeNull();
    }

    /// <summary>
    /// Vor dem Update ausgestellte Tokens haben keinen Versions-Claim und sollen bis zum ersten Widerruf gültig bleiben.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_AltesTokenOhneVersionsClaim_GiltAlsVersionNull()
    {
        var member = Save();
        var key = WerkbankJwt.SigningKey(new string('v', 32));
        var legacy = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            WerkbankJwt.DefaultIssuer, WerkbankJwt.DefaultIssuer,
            [new Claim(ClaimTypes.NameIdentifier, member.Id.ToString()), new Claim(ClaimTypes.Role, "Buero")],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new Microsoft.IdentityModel.Tokens.SigningCredentials(key, "HS256")));

        (await _validator.ValidateAsync(legacy)).Should().NotBeNull();

        Update(member.Id, m => m.RevokeSessions());
        (await _validator.ValidateAsync(legacy)).Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_ServiceTokenUndTokenFuerDeaktivierteAnmeldung_BrauchenKeinKonto()
    {
        (await _validator.ValidateAsync(new ServiceTokenProvider(Configuration).GetToken())).Should().NotBeNull();
        (await _validator.ValidateAsync(_jwt.GenerateAuthDisabledToken())).Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateAsync_UngueltigeSignatur_IstUngueltig()
    {
        (await _validator.ValidateAsync("kein-token")).Should().BeNull();
    }
}
