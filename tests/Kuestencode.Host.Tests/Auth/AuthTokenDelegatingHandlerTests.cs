using FluentAssertions;
using Kuestencode.Shared.UI.Auth;
using Kuestencode.Shared.UI.Handlers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Kuestencode.Host.Tests.Auth;

/// <summary>
/// Security-Audit H4: ausgehende Aufrufe tragen immer ein signiertes Token statt sich auf die Quell-IP zu verlassen.
/// </summary>
public class AuthTokenDelegatingHandlerTests
{
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }

    private static readonly ServiceTokenProvider ServiceTokens = new(new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Secret"] = new string('k', 32) })
        .Build());

    private static async Task<string?> SendWith(HttpContext? httpContext)
    {
        var capture = new CapturingHandler();
        var handler = new AuthTokenDelegatingHandler(new HttpContextAccessor { HttpContext = httpContext }, ServiceTokens)
        {
            InnerHandler = capture
        };

        await new HttpClient(handler).GetAsync("http://faktura:8081/api/Invoice");
        return capture.Request!.Headers.Authorization?.ToString();
    }

    [Fact]
    public async Task SendAsync_MitNutzerHeader_ReichtNutzerTokenWeiter()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer nutzer-token";

        (await SendWith(context)).Should().Be("Bearer nutzer-token");
    }

    [Fact]
    public async Task SendAsync_MitNutzerCookie_ReichtCookieAlsBearerWeiter()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Append("Cookie", $"{WerkbankJwt.CookieName}=cookie-token");

        (await SendWith(context)).Should().Be("Bearer cookie-token");
    }

    [Fact]
    public async Task SendAsync_OhneHttpContext_SchicktServiceToken()
    {
        (await SendWith(null)).Should().Be($"Bearer {ServiceTokens.GetToken()}");
    }

    [Fact]
    public async Task SendAsync_HttpContextOhneToken_SchicktServiceToken()
    {
        (await SendWith(new DefaultHttpContext())).Should().Be($"Bearer {ServiceTokens.GetToken()}");
    }
}
