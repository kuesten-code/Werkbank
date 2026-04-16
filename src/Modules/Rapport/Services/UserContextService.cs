using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;

namespace Kuestencode.Rapport.Services;

public class UserContextService : IUserContextService
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    // Circuit-level cache: populated on first successful resolve and kept for lifetime of the scoped service
    private ClaimsPrincipal? _cachedPrincipal;

    public UserContextService(AuthenticationStateProvider authStateProvider, IHttpContextAccessor httpContextAccessor)
    {
        _authStateProvider = authStateProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    private async Task<ClaimsPrincipal> GetPrincipalAsync()
    {
        if (_cachedPrincipal != null)
            return _cachedPrincipal;

        // 1. Try HttpContext directly (available during Prerender / API calls)
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            // Try cookie
            if (httpContext.Request.Cookies.TryGetValue("werkbank_auth_cookie", out var cookieToken)
                && !string.IsNullOrEmpty(cookieToken))
            {
                var p = ParseJwt(cookieToken);
                if (p != null) { _cachedPrincipal = p; return p; }
            }

            // Try Authorization header (set by YARP transform)
            var authHeader = httpContext.Request.Headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var p = ParseJwt(authHeader["Bearer ".Length..].Trim());
                if (p != null) { _cachedPrincipal = p; return p; }
            }

            // Try already-authenticated HttpContext.User
            if (httpContext.User?.Identity?.IsAuthenticated == true)
            {
                _cachedPrincipal = httpContext.User;
                return httpContext.User;
            }
        }

        // 2. Fallback: AuthStateProvider (works when HttpContext is null in SignalR circuit
        //    and _cachedState was populated during prerender)
        var state = await _authStateProvider.GetAuthenticationStateAsync();
        if (state.User.Identity?.IsAuthenticated == true)
        {
            _cachedPrincipal = state.User;
            return state.User;
        }

        return state.User;
    }

    public async Task<Guid?> GetCurrentUserIdAsync()
    {
        var user = await GetPrincipalAsync();
        var idClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(idClaim, out var id) && id != Guid.Empty)
            return id;
        return null;
    }

    public async Task<string?> GetCurrentUserNameAsync()
    {
        var user = await GetPrincipalAsync();
        return user.FindFirstValue(ClaimTypes.Name);
    }

    public async Task<string?> GetCurrentUserRoleAsync()
    {
        var user = await GetPrincipalAsync();
        return user.FindFirstValue(ClaimTypes.Role);
    }

    public async Task<bool> IsAdminOrBueroAsync()
    {
        var role = await GetCurrentUserRoleAsync();
        return role == "Admin" || role == "Buero";
    }

    public async Task<bool> IsAdminAsync()
    {
        var role = await GetCurrentUserRoleAsync();
        return role == "Admin";
    }

    private static ClaimsPrincipal? ParseJwt(string token)
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
}
