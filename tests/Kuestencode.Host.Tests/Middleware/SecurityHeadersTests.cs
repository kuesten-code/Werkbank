using FluentAssertions;
using Kuestencode.Shared.UI.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace Kuestencode.Host.Tests.Middleware;

public class SecurityHeadersTests
{
    /// <summary>
    /// Simuliert eine proxied Modul-Antwort, die die Header schon mitbringt: am Ende darf jeder Header genau einmal stehen.
    /// </summary>
    [Fact]
    public async Task UseWerkbankSecurityHeaders_HeaderVomModul_StehenNurEinmalInDerAntwort()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();
        app.UseWerkbankSecurityHeaders();
        app.Run(context =>
        {
            context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            return context.Response.WriteAsync("ok");
        });
        await app.StartAsync();

        var response = await app.GetTestClient().GetAsync("/");

        response.Headers.GetValues("X-Frame-Options").Should().ContainSingle().Which.Should().Be("SAMEORIGIN");
        response.Headers.GetValues("X-Content-Type-Options").Should().ContainSingle().Which.Should().Be("nosniff");
        response.Headers.GetValues("Referrer-Policy").Should().ContainSingle();
    }
}
