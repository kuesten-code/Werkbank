using System.Security.Claims;
using Kuestencode.Shared.UI.Auth;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Models;
using Kuestencode.Werkbank.Host.Services;
using Microsoft.EntityFrameworkCore;
using Yarp.ReverseProxy.Model;

namespace Kuestencode.Werkbank.Host.Middleware;

public class AuthMiddleware
{
    public const string AuthDisabledType = "NoAuth";

    private readonly RequestDelegate _next;
    private readonly ISessionValidator _sessionValidator;
    private readonly ILogger<AuthMiddleware> _logger;

    private static readonly HashSet<string> PublicPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/login",
        "/api/auth/forgot-password",
        "/api/auth/reset-password",
        "/api/setup/required",
        "/api/setup/complete",
        "/api/feedback/status"
    };

    private static readonly string[] PublicPathPrefixes = new[]
    {
        "/api/mobile/",  // Mobile API (Token-Status, PIN setzen/prüfen)
        "/m/",           // Mobile Blazor-Seiten
        "/api/v1/"       // Feedback-Hub-API: authentifiziert per X-Api-Key (FeedbackApiKeyFilter)
    };

    public AuthMiddleware(RequestDelegate next, ISessionValidator sessionValidator, ILogger<AuthMiddleware> logger)
    {
        _next = next;
        _sessionValidator = sessionValidator;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        var isModuleRoute = IsModuleRoute(context);

        // Statische Dateien, Blazor-Hub und öffentliche API-Pfade des Hosts durchlassen.
        // Modul-Routen nie: die Module verlassen sich darauf, dass der Host ohne Login nichts durchreicht.
        if (!isModuleRoute && IsPublicPath(path))
        {
            await _next(context);
            return;
        }

        // Auth-Status aus DB prüfen
        using var scope = context.RequestServices.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HostDbContext>();
        var settings = await dbContext.WerkbankSettings.AsNoTracking().FirstOrDefaultAsync();

        if (settings == null || !settings.AuthEnabled)
        {
            // Auth deaktiviert: impliziter Admin-User
            var adminClaims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, Guid.Empty.ToString()),
                new Claim(ClaimTypes.Name, "Admin"),
                new Claim(ClaimTypes.Role, UserRole.Admin.ToString())
            };
            context.User = new ClaimsPrincipal(
                new ClaimsIdentity(adminClaims, AuthDisabledType));

            await _next(context);
            return;
        }

        var token = WerkbankJwt.ExtractToken(context);

        // WICHTIG: Host ist die einzige nach außen (und im LAN) erreichbare Komponente —
        // anders als bei den Modulen (JwtUserContextMiddleware) darf hier NICHT anhand der
        // Quell-IP auf "intern" geschlossen werden: Docker leitet auch echten Browser-Traffic
        // von externen/LAN-Clients über den veröffentlichten Port so weiter, dass er wie ein
        // Docker-internes Netz aussieht (private IP-Bereiche). Ein solcher IP-Check hätte
        // externen Zugriff ohne Login erlaubt. Echte interne Technik-Aufrufe (Registrierung,
        // Healthcheck-Registrierung) weisen sich stattdessen mit einem Service-Token aus.

        // Auth aktiviert: JWT prüfen
        if (string.IsNullOrEmpty(token))
        {
            // API-Requests bekommen 401
            if (IsApiPath(path))
            {
                var remoteIp = context.Connection.RemoteIpAddress;
                var hostHeader = context.Request.Host.Host;
                _logger.LogWarning("AuthMiddleware: 401 no token. Path={Path}, RemoteIP={RemoteIP}, Host={Host}",
                    path, remoteIp, hostHeader);
                context.Response.StatusCode = 401;
                return;
            }

            await RejectPageOrContinue(context, path, isModuleRoute);
            return;
        }

        var principal = await _sessionValidator.ValidateAsync(token);
        if (principal == null)
        {
            // Ungültiger Token: API-Requests bekommen 401
            if (IsApiPath(path))
            {
                _logger.LogWarning("AuthMiddleware: 401 invalid token. Path={Path}, Host={Host}, HasAuthHeader={HasAuthHeader}",
                    path,
                    context.Request.Host.Host,
                    context.Request.Headers.ContainsKey("Authorization"));
                context.Response.StatusCode = 401;
                return;
            }

            await RejectPageOrContinue(context, path, isModuleRoute);
            return;
        }

        // Gültiger Token: User-Context setzen
        context.User = principal;
        await _next(context);
    }

    /// <summary>
    /// Host-Seiten leiten über ihren AuthStateProvider selbst zum Login um. Modul-Seiten werden per
    /// YARP durchgereicht und haben diese Prüfung nicht, daher leitet hier schon der Host um.
    /// </summary>
    private Task RejectPageOrContinue(HttpContext context, string path, bool isModuleRoute)
    {
        if (!isModuleRoute)
            return _next(context);

        var returnUrl = Uri.EscapeDataString(path + context.Request.QueryString);
        context.Response.Redirect($"/login?returnUrl={returnUrl}");
        return Task.CompletedTask;
    }

    private static bool IsModuleRoute(HttpContext context) =>
        context.GetEndpoint()?.Metadata.GetMetadata<RouteModel>() != null;

    /// <summary>
    /// Erkennt API-Aufrufe sowohl am Host selbst ("/api/...") als auch an über YARP
    /// weitergeleitete Modul-Endpunkte ("/{modul}/api/...", z. B. "/recepta/api/recepta/documents").
    /// Ohne diesen Check würden proxied Modul-API-Aufrufe fälschlich als "Page-Request"
    /// durchgelassen, wenn kein Token vorhanden ist (siehe fehlende [RequireRole]-Absicherung).
    /// </summary>
    private static bool IsApiPath(string path) =>
        path.Contains("/api/", StringComparison.OrdinalIgnoreCase);

    private static bool IsPublicPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return true;

        // Statische Dateien
        if (path.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/_content", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/company/logos", StringComparison.OrdinalIgnoreCase) ||
            (Path.HasExtension(path) && !IsApiPath(path)))
        {
            return true;
        }

        // Public Path Prefixes (z.B. /api/mobile/, /m/)
        if (PublicPathPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // Öffentliche Auth-Endpoints
        if (PublicPaths.Contains(path))
            return true;

        // Invite/Reset-Token-Validierung und -Annahme
        if (path.StartsWith("/api/team-members/invite/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/auth/reset/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/invite/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/reset/", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/login", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/login/mfa", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/auth/login/mfa", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/forgot-password", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
