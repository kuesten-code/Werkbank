using System.Security.Claims;
using FluentAssertions;
using Kuestencode.Werkbank.Host.Auth;
using Kuestencode.Werkbank.Host.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Xunit;
using UserRole = Kuestencode.Shared.Contracts.Host.UserRole;

namespace Kuestencode.Host.Tests.Auth;

public class RequireRoleAttributeTests
{
    private static IActionResult? Run(ClaimsPrincipal user, params UserRole[] allowed)
    {
        var httpContext = new DefaultHttpContext { User = user };
        var context = new AuthorizationFilterContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()), new List<IFilterMetadata>());
        new RequireRoleAttribute(allowed).OnAuthorization(context);
        return context.Result;
    }

    private static ClaimsPrincipal User(string authenticationType, string? role = null) =>
        new(new ClaimsIdentity(role == null ? [] : [new Claim(ClaimTypes.Role, role)], authenticationType));

    [Fact]
    public void AnmeldungDeaktiviert_LaesstDurch()
    {
        Run(User(AuthMiddleware.AuthDisabledType), UserRole.Admin).Should().BeNull();
    }

    [Fact]
    public void Anonym_Gibt401()
    {
        Run(new ClaimsPrincipal(new ClaimsIdentity()), UserRole.Admin).Should().BeOfType<UnauthorizedResult>();
    }

    /// <summary>
    /// Der frühere Sonderfall "InternalService" ist entfallen (Security-Audit H4): ohne Rolle kein Zugriff.
    /// </summary>
    [Fact]
    public void FruehererInternalServicePrincipalOhneRolle_Gibt403()
    {
        Run(User("InternalService"), UserRole.Admin)
            .Should().BeOfType<StatusCodeResult>().Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public void FalscheRolle_Gibt403()
    {
        Run(User("jwt", "Mitarbeiter"), UserRole.Admin, UserRole.Buero)
            .Should().BeOfType<StatusCodeResult>().Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public void PassendeRolle_LaesstDurch()
    {
        Run(User("jwt", "Buero"), UserRole.Admin, UserRole.Buero).Should().BeNull();
    }
}
