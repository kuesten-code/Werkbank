using Microsoft.AspNetCore.Http;

namespace Kuestencode.Shared.UI.Auth;

/// <summary>
/// Setzt HttpContext.User für API-Requests im Modul aus einem signaturgeprüften Nutzer- oder Service-Token.
/// Blazor-Circuits ermitteln die Identität separat über <see cref="PassThroughAuthStateProvider"/>.
/// </summary>
public class JwtUserContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly JwtPrincipalValidator _validator;

    public JwtUserContextMiddleware(RequestDelegate next, JwtPrincipalValidator validator)
    {
        _next = next;
        _validator = validator;
    }

    public Task InvokeAsync(HttpContext context)
    {
        var token = WerkbankJwt.ExtractToken(context);
        var principal = token != null ? _validator.Validate(token) : null;
        if (principal != null)
            context.User = principal;

        return _next(context);
    }
}
