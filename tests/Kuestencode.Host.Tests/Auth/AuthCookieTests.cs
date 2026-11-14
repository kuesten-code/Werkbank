using FluentAssertions;
using Kuestencode.Werkbank.Host.Auth;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Kuestencode.Host.Tests.Auth;

/// <summary>
/// Härtung aus dem Security-Audit: Web- und Mobile-Login setzen das Session-Cookie mit denselben Schutz-Flags.
/// </summary>
public class AuthCookieTests
{
    [Fact]
    public void Append_SetztCookieNurFuerHttpsUndOhneJavaScriptZugriff()
    {
        var context = new DefaultHttpContext();

        AuthCookie.Append(context.Response, "token", TimeSpan.FromDays(30));

        var header = context.Response.Headers.SetCookie.ToString().ToLowerInvariant();
        header.Should().StartWith("werkbank_auth_cookie=token");
        header.Should().Contain("secure").And.Contain("httponly").And.Contain("samesite=lax").And.Contain("path=/");
    }

    [Fact]
    public void Delete_LaesstCookieSofortAblaufen()
    {
        var context = new DefaultHttpContext();

        AuthCookie.Delete(context.Response);

        context.Response.Headers.SetCookie.ToString().Should().Contain("werkbank_auth_cookie=;").And.Contain("expires=Thu, 01 Jan 1970");
    }
}
