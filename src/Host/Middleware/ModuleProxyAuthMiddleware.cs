using Kuestencode.Shared.UI.Auth;
using Kuestencode.Werkbank.Host.Services;

namespace Kuestencode.Werkbank.Host.Middleware;

/// <summary>
/// Läuft in der YARP-Pipeline nach der <see cref="AuthMiddleware"/> und gibt dem Modul das Token mit, das der
/// Host geprüft hat. Bei deaktivierter Anmeldung stellt der Host ein kurzlebiges Admin-Token aus, damit die
/// Module unabhängig vom Modus immer ein signiertes Token verlangen können.
/// </summary>
public class ModuleProxyAuthMiddleware
{
    private readonly RequestDelegate _next;

    public ModuleProxyAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context, IJwtTokenService jwtTokenService)
    {
        var token = context.User.Identity?.AuthenticationType == AuthMiddleware.AuthDisabledType
            ? jwtTokenService.GenerateAuthDisabledToken()
            : WerkbankJwt.ExtractToken(context);

        if (token != null)
            context.Request.Headers.Authorization = $"Bearer {token}";

        return _next(context);
    }
}

public static class ModuleProxyAuthExtensions
{
    public static IApplicationBuilder UseModuleProxyAuth(this IApplicationBuilder app) =>
        app.UseMiddleware<ModuleProxyAuthMiddleware>();
}
