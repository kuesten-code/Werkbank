using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Kuestencode.Shared.UI.Auth;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Kuestencode.Host.Tests.Auth;

public class ServiceTokenProviderTests
{
    private const string Secret = "service-token-tests-secret-0123456789ab";

    private sealed class ManualTimeProvider(DateTimeOffset start) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = start;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static IConfiguration Config(string? secret) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Secret"] = secret })
        .Build();

    [Fact]
    public void GetToken_TraegtServiceAudienceUndKurzeLaufzeit()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UtcNow);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(new ServiceTokenProvider(Config(Secret), time).GetToken());

        jwt.Audiences.Should().ContainSingle().Which.Should().Be(WerkbankJwt.ServiceAudience(WerkbankJwt.DefaultIssuer));
        jwt.ValidTo.Should().BeCloseTo(time.Now.UtcDateTime.AddMinutes(5), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void GetToken_InnerhalbDerLaufzeit_LiefertGecachtesToken()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UtcNow);
        var provider = new ServiceTokenProvider(Config(Secret), time);

        var first = provider.GetToken();
        time.Now = time.Now.AddMinutes(3);

        provider.GetToken().Should().Be(first);
    }

    [Fact]
    public void GetToken_KurzVorAblauf_StelltNeuesTokenAus()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UtcNow);
        var provider = new ServiceTokenProvider(Config(Secret), time);

        var first = provider.GetToken();
        time.Now = time.Now.AddMinutes(4).AddSeconds(30);

        provider.GetToken().Should().NotBe(first);
    }

    [Fact]
    public void Konstruktor_OhneSecret_Wirft()
    {
        var act = () => new ServiceTokenProvider(Config(null));

        act.Should().Throw<InvalidOperationException>();
    }
}
