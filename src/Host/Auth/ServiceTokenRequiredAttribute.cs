using Kuestencode.Shared.UI.Auth;
using Kuestencode.Werkbank.Host.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kuestencode.Werkbank.Host.Auth;

/// <summary>
/// Lässt nur Server-zu-Server-Aufrufe mit gültigem Service-Token durch. Prüft das Token selbst, weil die
/// AuthMiddleware bei deaktivierter Anmeldung jeden Request als impliziten Admin durchwinkt.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ServiceTokenRequiredAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var token = WerkbankJwt.ExtractToken(context.HttpContext);
        var principal = token != null
            ? context.HttpContext.RequestServices.GetRequiredService<IJwtTokenService>().ValidateToken(token)
            : null;

        if (principal?.HasClaim(WerkbankJwt.ServiceClaim, "true") != true)
            context.Result = new UnauthorizedResult();
    }
}
