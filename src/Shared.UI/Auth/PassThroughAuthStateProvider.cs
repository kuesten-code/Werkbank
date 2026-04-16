using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Kuestencode.Shared.UI.Auth;

/// <summary>
/// AuthenticationStateProvider for modules that reads the JWT token from the
/// werkbank_auth_cookie and parses the claims directly.
/// Uses PersistentComponentState to bridge the Prerender → SignalR-Circuit gap.
/// </summary>
public class PassThroughAuthStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<PassThroughAuthStateProvider> _logger;
    private readonly PersistentComponentState _persistentState;
    private readonly PersistingComponentStateSubscription _subscription;

    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());
    private Task<AuthenticationState>? _authStateTask;

    public PassThroughAuthStateProvider(
        IHttpContextAccessor httpContextAccessor,
        ILogger<PassThroughAuthStateProvider> logger,
        PersistentComponentState persistentState)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _persistentState = persistentState;

        // Subscribe to persist state before circuit teardown (Prerender phase)
        _subscription = persistentState.RegisterOnPersisting(PersistAuthState);

        _authStateTask = ResolveAuthStateAsync();
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => _authStateTask!;

    private Task PersistAuthState()
    {
        // During Prerender: save the resolved token so the Circuit can restore it
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return Task.CompletedTask;

        string? token = null;

        if (httpContext.Request.Cookies.TryGetValue("werkbank_auth_cookie", out var cookieToken)
            && !string.IsNullOrEmpty(cookieToken))
        {
            token = cookieToken;
        }
        else
        {
            var authHeader = httpContext.Request.Headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                token = authHeader["Bearer ".Length..].Trim();
        }

        if (!string.IsNullOrEmpty(token))
            _persistentState.PersistAsJson("werkbank_jwt", token);

        return Task.CompletedTask;
    }

    private Task<AuthenticationState> ResolveAuthStateAsync()
    {
        // 1. Try HttpContext (Prerender / direct HTTP request)
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var state = ResolveFromHttpContext(httpContext);
            return Task.FromResult(state);
        }

        // 2. Try PersistentComponentState (SignalR circuit after Prerender)
        if (_persistentState.TryTakeFromJson<string>("werkbank_jwt", out var persistedToken)
            && !string.IsNullOrEmpty(persistedToken))
        {
            var principal = ParseJwtToken(persistedToken);
            if (principal != null)
            {
                _logger.LogDebug("PassThrough: Restored JWT from PersistentComponentState. Role={Role}",
                    principal.FindFirstValue(ClaimTypes.Role));
                return Task.FromResult(new AuthenticationState(principal));
            }
        }

        _logger.LogWarning("PassThrough: No HttpContext and no persisted state — returning anonymous");
        return Task.FromResult(new AuthenticationState(Anonymous));
    }

    private AuthenticationState ResolveFromHttpContext(HttpContext httpContext)
    {
        // Try cookie
        if (httpContext.Request.Cookies.TryGetValue("werkbank_auth_cookie", out var token)
            && !string.IsNullOrEmpty(token))
        {
            var principal = ParseJwtToken(token);
            if (principal != null)
            {
                _logger.LogInformation("PassThrough: JWT parsed from cookie. Role={Role}",
                    principal.FindFirstValue(ClaimTypes.Role));
                return new AuthenticationState(principal);
            }
            _logger.LogWarning("PassThrough: werkbank_auth_cookie found but JWT parsing failed");
        }

        // Try Authorization header (set by YARP transform)
        var authHeader = httpContext.Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var principal = ParseJwtToken(authHeader["Bearer ".Length..].Trim());
            if (principal != null)
            {
                _logger.LogInformation("PassThrough: JWT parsed from Authorization header. Role={Role}",
                    principal.FindFirstValue(ClaimTypes.Role));
                return new AuthenticationState(principal);
            }
            _logger.LogWarning("PassThrough: Authorization header found but JWT parsing failed");
        }

        // Fallback: HttpContext.User (set by middleware)
        if (httpContext.User?.Identity?.IsAuthenticated == true)
        {
            _logger.LogInformation("PassThrough: Using HttpContext.User. AuthType={AuthType}",
                httpContext.User.Identity.AuthenticationType);
            return new AuthenticationState(httpContext.User);
        }

        _logger.LogInformation("PassThrough: No auth found — anonymous");
        return new AuthenticationState(Anonymous);
    }

    private static ClaimsPrincipal? ParseJwtToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            if (jwt.ValidTo < DateTime.UtcNow) return null;

            var mappedClaims = jwt.Claims.Select(c => new Claim(
                c.Type switch
                {
                    "role"   => ClaimTypes.Role,
                    "name"   => ClaimTypes.Name,
                    "nameid" => ClaimTypes.NameIdentifier,
                    "email"  => ClaimTypes.Email,
                    _        => c.Type
                }, c.Value));

            return new ClaimsPrincipal(new ClaimsIdentity(mappedClaims, "jwt"));
        }
        catch { return null; }
    }

    public void Dispose() => _subscription.Dispose();
}
