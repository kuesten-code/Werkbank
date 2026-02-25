using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Kuestencode.Shared.UI.Auth;

/// <summary>
/// AuthenticationStateProvider for modules that reads the JWT token from the
/// werkbank_auth_cookie and parses the claims directly.
/// Used when modules run as separate services behind YARP reverse proxy.
/// </summary>
public class PassThroughAuthStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<PassThroughAuthStateProvider> _logger;

    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

    // Cache the auth state from the initial HTTP request (prerender)
    // so it survives into the SignalR circuit where HttpContext is null.
    private AuthenticationState? _cachedState;

    public PassThroughAuthStateProvider(
        IHttpContextAccessor httpContextAccessor,
        ILogger<PassThroughAuthStateProvider> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext != null)
        {
            // We have an HTTP context (prerender or initial request) — resolve and cache
            var state = ResolveFromHttpContext(httpContext);
            _cachedState = state;
            return Task.FromResult(state);
        }

        // No HTTP context (SignalR circuit) — return cached state from prerender
        if (_cachedState != null)
        {
            _logger.LogDebug("PassThrough: No HttpContext (SignalR), using cached state. Authenticated={IsAuth}, Role={Role}",
                _cachedState.User.Identity?.IsAuthenticated,
                _cachedState.User.FindFirstValue(ClaimTypes.Role));
            return Task.FromResult(_cachedState);
        }

        _logger.LogWarning("PassThrough: No HttpContext and no cached state — returning anonymous");
        return Task.FromResult(new AuthenticationState(Anonymous));
    }

    private AuthenticationState ResolveFromHttpContext(HttpContext httpContext)
    {
        var cookieCount = httpContext.Request.Cookies.Count;
        var hasCookie = httpContext.Request.Cookies.ContainsKey("werkbank_auth_cookie");
        var path = httpContext.Request.Path.Value;

        var cookieNames = string.Join(", ", httpContext.Request.Cookies.Keys);
        _logger.LogInformation("PassThrough: HttpContext available. Path={Path}, Cookies={Count}, HasAuthCookie={HasCookie}, CookieNames=[{CookieNames}]",
            path, cookieCount, hasCookie, cookieNames);

        // Try JWT token from cookie
        if (httpContext.Request.Cookies.TryGetValue("werkbank_auth_cookie", out var token)
            && !string.IsNullOrEmpty(token))
        {
            var principal = ParseJwtToken(token);
            if (principal != null)
            {
                var role = principal.FindFirstValue(ClaimTypes.Role);
                _logger.LogInformation("PassThrough: JWT parsed from cookie. Role={Role}", role);
                return new AuthenticationState(principal);
            }

            _logger.LogWarning("PassThrough: werkbank_auth_cookie found but JWT parsing failed");
        }

        // Try JWT token from Authorization header (set by YARP transform)
        var authHeader = httpContext.Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var headerToken = authHeader["Bearer ".Length..].Trim();
            var principal = ParseJwtToken(headerToken);
            if (principal != null)
            {
                var role = principal.FindFirstValue(ClaimTypes.Role);
                _logger.LogInformation("PassThrough: JWT parsed from Authorization header. Role={Role}", role);
                return new AuthenticationState(principal);
            }

            _logger.LogWarning("PassThrough: Authorization header found but JWT parsing failed");
        }

        // Fallback: check if User was already set (e.g. by middleware)
        if (httpContext.User?.Identity?.IsAuthenticated == true)
        {
            _logger.LogInformation("PassThrough: Using HttpContext.User (set by middleware). AuthType={AuthType}",
                httpContext.User.Identity.AuthenticationType);
            return new AuthenticationState(httpContext.User);
        }

        _logger.LogInformation("PassThrough: No auth cookie and no authenticated user — anonymous");
        return new AuthenticationState(Anonymous);
    }

    private static ClaimsPrincipal? ParseJwtToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            if (jwt.ValidTo < DateTime.UtcNow)
                return null;

            // Map short JWT claim types (e.g. "role") to .NET ClaimTypes (e.g. ClaimTypes.Role)
            var mappedClaims = jwt.Claims.Select(c => new Claim(
                c.Type switch
                {
                    "role" => ClaimTypes.Role,
                    "name" => ClaimTypes.Name,
                    "nameid" => ClaimTypes.NameIdentifier,
                    "email" => ClaimTypes.Email,
                    _ => c.Type
                },
                c.Value));

            var identity = new ClaimsIdentity(mappedClaims, "jwt");
            return new ClaimsPrincipal(identity);
        }
        catch
        {
            return null;
        }
    }
}
