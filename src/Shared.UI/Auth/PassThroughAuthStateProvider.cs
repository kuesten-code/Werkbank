using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;

namespace Kuestencode.Shared.UI.Auth;

/// <summary>
/// AuthenticationStateProvider for modules behind YARP reverse proxy.
/// Prerender: reads JWT from cookie/Authorization header via HttpContext.
/// SignalR circuit: reads JWT from window.__werkbank_jwt via JS-Interop.
/// </summary>
public class PassThroughAuthStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<PassThroughAuthStateProvider> _logger;

    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

    // Populated during Prerender so the circuit can skip JS-Interop if already resolved
    private AuthenticationState? _resolved;

    public PassThroughAuthStateProvider(
        IHttpContextAccessor httpContextAccessor,
        IJSRuntime jsRuntime,
        ILogger<PassThroughAuthStateProvider> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // Already resolved (Prerender ran first in this scope — unlikely for Server but safe)
        if (_resolved != null)
            return _resolved;

        // 1. Try HttpContext (Prerender / direct HTTP request)
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var token = ExtractTokenFromHttpContext(httpContext);
            if (!string.IsNullOrEmpty(token))
            {
                var principal = ParseJwt(token);
                if (principal != null)
                {
                    _logger.LogInformation("PassThrough: JWT from HttpContext. Role={Role}",
                        principal.FindFirstValue(ClaimTypes.Role));
                    _resolved = new AuthenticationState(principal);
                    return _resolved;
                }
            }

            if (httpContext.User?.Identity?.IsAuthenticated == true)
            {
                _resolved = new AuthenticationState(httpContext.User);
                return _resolved;
            }
        }

        // 2. SignalR circuit: read from window.__werkbank_jwt set by _Host.cshtml
        try
        {
            var jwt = await _jsRuntime.InvokeAsync<string?>("getWerkbankJwt");
            if (!string.IsNullOrEmpty(jwt))
            {
                var principal = ParseJwt(jwt);
                if (principal != null)
                {
                    _logger.LogInformation("PassThrough: JWT from JS window.__werkbank_jwt. Role={Role}",
                        principal.FindFirstValue(ClaimTypes.Role));
                    _resolved = new AuthenticationState(principal);
                    return _resolved;
                }
            }
        }
        catch (Exception ex)
        {
            // JS-Interop not yet available (e.g. during prerender of a different component)
            _logger.LogDebug("PassThrough: JS-Interop not available: {Message}", ex.Message);
        }

        _logger.LogWarning("PassThrough: No auth found — returning anonymous");
        return new AuthenticationState(Anonymous);
    }

    private string? ExtractTokenFromHttpContext(HttpContext httpContext)
    {
        _logger.LogInformation(
            "PassThrough: HttpContext available. Path={Path}, HasAuthCookie={HasCookie}, HasAuthHeader={HasHeader}",
            httpContext.Request.Path,
            httpContext.Request.Cookies.ContainsKey("werkbank_auth_cookie"),
            httpContext.Request.Headers.ContainsKey("Authorization"));

        if (httpContext.Request.Cookies.TryGetValue("werkbank_auth_cookie", out var cookie)
            && !string.IsNullOrEmpty(cookie))
            return cookie;

        var authHeader = httpContext.Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return authHeader["Bearer ".Length..].Trim();

        return null;
    }

    private static ClaimsPrincipal? ParseJwt(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            if (jwt.ValidTo < DateTime.UtcNow) return null;

            var claims = jwt.Claims.Select(c => new Claim(
                c.Type switch
                {
                    "role"   => ClaimTypes.Role,
                    "name"   => ClaimTypes.Name,
                    "nameid" => ClaimTypes.NameIdentifier,
                    "email"  => ClaimTypes.Email,
                    _        => c.Type
                }, c.Value));

            return new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
        }
        catch { return null; }
    }
}
