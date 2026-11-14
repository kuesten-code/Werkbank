using FluentAssertions;
using Kuestencode.Shared.UI.Auth;
using Kuestencode.Werkbank.Host.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Kuestencode.Host.Tests.Auth;

public class LoginHardeningTests
{
    [Theory]
    [InlineData("/faktura/invoices?status=open", "/faktura/invoices?status=open")]
    [InlineData("/", "/")]
    [InlineData(null, "/")]
    [InlineData("", "/")]
    [InlineData("https://phishing.example/login", "/")]
    [InlineData("//phishing.example", "/")]
    [InlineData("/\\phishing.example", "/")]
    [InlineData("javascript:alert(1)", "/")]
    [InlineData("/ok\r\nLocation: x", "/")]
    public void LocalReturnUrl_NurEigenePfade(string? input, string expected)
    {
        LocalReturnUrl.Sanitize(input).Should().Be(expected);
    }

    [Fact]
    public void ClientKey_HinterProxy_NimmtErstenWeitergeleitetenClient()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.7, 10.0.0.2";
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("172.18.0.1");

        AuthRateLimit.ClientKey(context).Should().Be("203.0.113.7");
    }

    [Fact]
    public void ClientKey_OhneProxy_NimmtVerbindungsAdresse()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.20");

        AuthRateLimit.ClientKey(context).Should().Be("192.168.1.20");
    }

    [Theory]
    [InlineData("abcdefghijklmnopqrstuvwxyz123456")]
    [InlineData("change_me_use_a_long_random_string_here")]
    public void RequireSecret_BekannterBeispielwert_WirdAbgelehnt(string secret)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Secret"] = secret })
            .Build();

        var act = () => WerkbankJwt.RequireSecret(configuration);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Beispielwert*");
    }
}
