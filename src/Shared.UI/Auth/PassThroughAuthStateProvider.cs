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
    private readonly ILogger<PassThroughAuthStateProvider> _logger;
    private readonly PersistentComponentState _persistentState;
    private readonly PersistingComponentStateSubscription _subscription;

    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());
    private const string PersistKey = "werkbank_jwt";

    private readonly Task<AuthenticationState> _authStateTask;

    public PassThroughAuthStateProvider(
        IHttpContextAccessor httpContextAccessor,
        ILogger<PassThroughAuthStateProvider> logger,
        PersistentComponentState persistentState)
    {
        _logger = logger;
        _persistentState = persistentState;

        // Try to restore from persisted state first (SignalR circuit after Prerender)
        if (_persistentState.TryTakeFromJson<string>(PersistKey, out var persistedToken)
            && !string.IsNullOrEmpty(persistedToken))
        {
            var principal = ParseJwtToken(persistedToken);
            if (principal != null)
            {
                _logger.LogDebug("PassThrough: Restored JWT from PersistentComponentState. Role={Role}",
                    principal.FindFirstValue(ClaimTypes.Role));
                _authStateTask = Task.FromResult(new AuthenticationState(principal));
                _subscription = persistentState.RegisterOnPersisting(() => Task.CompletedTask);
                return;
            }
        }

        // Prerender / direct HTTP request: read from HttpContext and persist for the circuit
        var httpContext = httpContextAccessor.HttpContext;
        var token = ExtractToken(httpContext);

        // Register persisting callback — captures token by value
        _subscription = persistentState.RegisterOnPersisting(() =>
        {
            if (!string.IsNullOrEmpty(token))
                _persistentState.PersistAsJson(PersistKey, token);
            return Task.CompletedTask;
        });

        if (!string.IsNullOrEmpty(token))
        {
            var principal = ParseJwtToken(token);
            if (principal != null)
            {
                _logger.LogInformation("PassThrough: JWT resolved from HttpContext. Role={Role}",
                    principal.FindFirstValue(ClaimTypes.Role));
                _authStateTask = Task.FromResult(new AuthenticationState(principal));
                return;
            }
        }

        // Fallback: already-authenticated HttpContext.User
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            _logger.LogInformation("PassThrough: Using HttpContext.User. AuthType={AuthType}",
                httpContext.User.Identity.AuthenticationType);
            _authStateTask = Task.FromResult(new AuthenticationState(httpContext.User));
            return;
        }

        _logger.LogWarning("PassThrough: No auth found — returning anonymous");
        _authStateTask = Task.FromResult(new AuthenticationState(Anonymous));
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => _authStateTask;

    private static string? ExtractToken(HttpContext? httpContext)
    {
        if (httpContext == null) return null;

        if (httpContext.Request.Cookies.TryGetValue("werkbank_auth_cookie", out var cookieToken)
            && !string.IsNullOrEmpty(cookieToken))
            return cookieToken;

        var authHeader = httpContext.Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return authHeader["Bearer ".Length..].Trim();

        return null;
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
