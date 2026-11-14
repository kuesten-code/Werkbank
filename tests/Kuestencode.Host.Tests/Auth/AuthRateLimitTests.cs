using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Kuestencode.Werkbank.Host.Auth;
using Kuestencode.Werkbank.Host.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kuestencode.Host.Tests.Auth;

/// <summary>
/// Security-Audit M2/H1: Anmelde-Endpunkte werden pro Client gedrosselt, mit JSON-Antwort für die Login-Seite.
/// </summary>
public class AuthRateLimitTests
{
    private static async Task<HttpClient> StartAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRateLimiter(AuthRateLimit.Configure);
        var app = builder.Build();
        app.UseRateLimiter();
        app.MapPost("/login", () => Results.Ok()).RequireRateLimiting(AuthRateLimit.PolicyName);
        await app.StartAsync();
        return app.GetTestClient();
    }

    private static HttpRequestMessage Request(string clientIp)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/login");
        request.Headers.Add("X-Forwarded-For", clientIp);
        return request;
    }

    [Fact]
    public async Task UeberDemLimit_Gibt429MitJsonFehler()
    {
        var client = await StartAsync();
        for (var i = 0; i < AuthRateLimit.PermitsPerMinute; i++)
            (await client.SendAsync(Request("203.0.113.1"))).StatusCode.Should().Be(HttpStatusCode.OK);

        var blocked = await client.SendAsync(Request("203.0.113.1"));

        blocked.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        (await blocked.Content.ReadFromJsonAsync<Dictionary<string, string>>())!["error"].Should().Contain("Zu viele");
    }

    [Fact]
    public async Task AndererClient_HatEigenesKontingent()
    {
        var client = await StartAsync();
        for (var i = 0; i <= AuthRateLimit.PermitsPerMinute; i++)
            await client.SendAsync(Request("203.0.113.1"));

        (await client.SendAsync(Request("198.51.100.2"))).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData(nameof(AuthController.Login))]
    [InlineData(nameof(AuthController.LoginMfa))]
    [InlineData(nameof(AuthController.ForgotPassword))]
    [InlineData(nameof(AuthController.ResetPassword))]
    public void AnonymeAuthEndpunkte_SindGedrosselt(string action)
    {
        typeof(AuthController).GetMethod(action)!.GetCustomAttributes(typeof(EnableRateLimitingAttribute), false)
            .Should().ContainSingle().Which.As<EnableRateLimitingAttribute>().PolicyName.Should().Be(AuthRateLimit.PolicyName);
    }

    [Theory]
    [InlineData(nameof(MobileController.SetPin))]
    [InlineData(nameof(MobileController.VerifyPin))]
    public void MobilePinEndpunkte_SindGedrosselt(string action)
    {
        typeof(MobileController).GetMethod(action)!.GetCustomAttributes(typeof(EnableRateLimitingAttribute), false)
            .Should().ContainSingle();
    }

    [Fact]
    public void MitarbeiterRollenLesen_NurAdminUndBuero()
    {
        typeof(MitarbeiterRollenController).GetMethod(nameof(MitarbeiterRollenController.GetAll))!
            .GetCustomAttributes(typeof(RequireRoleAttribute), false)
            .Should().ContainSingle().Which.As<RequireRoleAttribute>().AllowedRoles
            .Should().BeEquivalentTo([Kuestencode.Shared.Contracts.Host.UserRole.Admin, Kuestencode.Shared.Contracts.Host.UserRole.Buero]);
    }
}
