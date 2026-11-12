using FluentAssertions;
using Kuestencode.Shared.UI.Auth;
using Kuestencode.Werkbank.Host.Auth;
using Kuestencode.Werkbank.Host.Models;
using Kuestencode.Werkbank.Host.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using UserRole = Kuestencode.Shared.Contracts.Host.UserRole;

namespace Kuestencode.Host.Tests.Auth;

/// <summary>
/// Security-Audit H2: Modul-Registrierung nur noch mit Service-Token.
/// </summary>
public class ServiceTokenRequiredAttributeTests
{
    private static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Secret"] = new string('r', 32) })
        .Build();

    private static readonly JwtTokenService JwtService = new(Configuration, NullLogger<JwtTokenService>.Instance);

    private static AuthorizationFilterContext Run(string? bearer)
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddSingleton<IJwtTokenService>(JwtService).BuildServiceProvider()
        };
        if (bearer != null)
            httpContext.Request.Headers.Authorization = $"Bearer {bearer}";

        var context = new AuthorizationFilterContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()), new List<IFilterMetadata>());
        new ServiceTokenRequiredAttribute().OnAuthorization(context);
        return context;
    }

    [Fact]
    public void OnAuthorization_GueltigesServiceToken_LaesstDurch()
    {
        Run(new ServiceTokenProvider(Configuration).GetToken()).Result.Should().BeNull();
    }

    [Fact]
    public void OnAuthorization_OhneToken_Gibt401()
    {
        Run(null).Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public void OnAuthorization_AdminSessionToken_Gibt401()
    {
        var adminToken = JwtService.GenerateToken(new TeamMember { Id = Guid.NewGuid(), DisplayName = "Admin", Role = UserRole.Admin });

        Run(adminToken).Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public void OnAuthorization_TokenFuerDeaktivierteAnmeldung_Gibt401()
    {
        Run(JwtService.GenerateAuthDisabledToken()).Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public void OnAuthorization_ServiceTokenMitFremdemSecret_Gibt401()
    {
        var foreign = new ServiceTokenProvider(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Secret"] = new string('x', 32) })
            .Build()).GetToken();

        Run(foreign).Result.Should().BeOfType<UnauthorizedResult>();
    }
}
