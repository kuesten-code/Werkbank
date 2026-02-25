using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Kuestencode.Werkbank.Host.Middleware;

public class AuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthMiddleware> _logger;

    private static readonly HashSet<string> PublicPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/login",
        "/api/auth/forgot-password",
        "/api/auth/reset-password",
        "/api/modules/register",
        "/api/modules/health",
        "/api/setup/required",
        "/api/setup/complete"
    };

    private static readonly string[] PublicPathPrefixes = new[]
    {
        "/api/mobile/",  // Mobile API (Token-Status, PIN setzen/prüfen)
        "/m/"            // Mobile Blazor-Seiten
    };

    public AuthMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<AuthMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        // Statische Dateien, Blazor-Hub und öffentliche API-Pfade durchlassen
        if (IsPublicPath(path))
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
                new ClaimsIdentity(adminClaims, "NoAuth"));

            await _next(context);
            return;
        }

        var token = ExtractToken(context);

        // Interne Modul-Kommunikation ohne User-Token durchlassen (z.B. Health/technische Calls).
        // Wenn ein Token vorhanden ist, wird IMMER validiert.
        var isInternal = IsInternalModuleRequest(context);
        if (isInternal && string.IsNullOrEmpty(token))
        {
            context.User = CreateInternalServicePrincipal();
            _logger.LogDebug("AuthMiddleware: Internal request without token uses internal service principal. Path={Path}", path);
            await _next(context);
            return;
        }

        // Auth aktiviert: JWT prüfen
        if (string.IsNullOrEmpty(token))
        {
            // API-Requests bekommen 401
            if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            {
                var remoteIp = context.Connection.RemoteIpAddress;
                var hostHeader = context.Request.Host.Host;
                _logger.LogWarning("AuthMiddleware: 401 no token. Path={Path}, RemoteIP={RemoteIP}, Host={Host}",
                    path, remoteIp, hostHeader);
                context.Response.StatusCode = 401;
                return;
            }

            // Blazor/Page-Requests durchlassen (AuthStateProvider handled redirect)
            await _next(context);
            return;
        }

        var principal = ValidateToken(token);
        if (principal == null)
        {
            if (isInternal)
            {
                context.User = CreateInternalServicePrincipal();
                _logger.LogWarning("AuthMiddleware: Invalid token on internal request. Using internal service principal. Path={Path}, Host={Host}",
                    path,
                    context.Request.Host.Host);
                await _next(context);
                return;
            }

            // Ungültiger Token: API-Requests bekommen 401
            if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("AuthMiddleware: 401 invalid token. Path={Path}, Host={Host}, HasAuthHeader={HasAuthHeader}",
                    path,
                    context.Request.Host.Host,
                    context.Request.Headers.ContainsKey("Authorization"));
                context.Response.StatusCode = 401;
                return;
            }

            await _next(context);
            return;
        }

        // Gültiger Token: User-Context setzen
        context.User = principal;
        await _next(context);
    }

    private static bool IsPublicPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return true;

        // Statische Dateien
        if (path.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/_content", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/company/logos", StringComparison.OrdinalIgnoreCase) ||
            path.Contains('.'))
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
            path.Equals("/forgot-password", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private bool IsInternalModuleRequest(HttpContext context)
    {
        // Requests von internen Modulen durchlassen (nicht vom Browser)
        // Module kommunizieren direkt über Docker-Netzwerk (host:8080)
        // Browser-Requests kommen über den externen Port (localhost:8080)
        var host = context.Request.Host.Host;

        // Docker-interne Requests: Hostname ist "host" (Docker-Service-Name) statt "localhost"
        if (string.Equals(host, "host", StringComparison.OrdinalIgnoreCase))
            return true;

        // Also check for other Docker service names (faktura, rapport, etc.)
        var knownServiceNames = new[] { "faktura", "rapport", "offerte", "acta", "recepta", "postgres" };
        if (knownServiceNames.Any(name => string.Equals(host, name, StringComparison.OrdinalIgnoreCase)))
            return true;

        // Check remote IP — handle both IPv4 and IPv6-mapped IPv4 (::ffff:172.x.x.x)
        var remoteIp = context.Connection.RemoteIpAddress;
        if (remoteIp != null)
        {
            // Normalize IPv6-mapped IPv4 to plain IPv4
            var ip = remoteIp.IsIPv4MappedToIPv6
                ? remoteIp.MapToIPv4().ToString()
                : remoteIp.ToString();

            if (ip.StartsWith("172.") || ip.StartsWith("10.") || ip.StartsWith("192.168."))
                return true;

            // Also check for IPv6 link-local (fe80::) and unique-local (fd/fc)
            if (ip.StartsWith("fe80:") || ip.StartsWith("fd") || ip.StartsWith("fc"))
                return true;
        }

        var path = context.Request.Path.Value;
        _logger.LogDebug("AuthMiddleware: IsInternalModuleRequest=false. Path={Path}, Host={Host}, RemoteIP={RemoteIP}",
            path, host, remoteIp);
        return false;
    }

    private static string? ExtractToken(HttpContext context)
    {
        // First try Authorization header
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader["Bearer ".Length..].Trim();
        }

        // Fallback to cookie
        if (context.Request.Cookies.TryGetValue("werkbank_auth_cookie", out var cookieToken))
        {
            return cookieToken;
        }

        return null;
    }

    private ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var secret = GetJwtSecret();
            if (string.IsNullOrEmpty(secret))
                return null;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var issuer = _configuration["Jwt:Issuer"] ?? "KuestencodeWerkbank";

            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = issuer,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, parameters, out _);
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "JWT-Validierung fehlgeschlagen");
            return null;
        }
    }

    private string? GetJwtSecret()
    {
        var secret = _configuration["Jwt:Secret"];
        if (!string.IsNullOrWhiteSpace(secret) && secret.Length >= 32)
            return secret;

        var filePath = Path.Combine(AppContext.BaseDirectory, "data", "jwt-secret.txt");
        if (File.Exists(filePath))
        {
            var stored = File.ReadAllText(filePath).Trim();
            if (stored.Length >= 32)
                return stored;
        }

        return null;
    }

    private static ClaimsPrincipal CreateInternalServicePrincipal()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.Empty.ToString()),
            new Claim(ClaimTypes.Name, "InternalModule"),
            new Claim(ClaimTypes.Role, UserRole.Admin.ToString())
        };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "InternalService"));
    }
}
