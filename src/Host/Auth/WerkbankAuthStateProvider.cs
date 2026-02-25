using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Kuestencode.Werkbank.Host.Models;

namespace Kuestencode.Werkbank.Host.Auth;

public class WerkbankAuthStateProvider : AuthenticationStateProvider
{
    private readonly ProtectedLocalStorage _localStorage;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WerkbankAuthStateProvider> _logger;

    private const string TokenKey = "werkbank_auth_token";
    private const string CookieName = "werkbank_auth_cookie";

    // Cache the auth state from the initial HTTP request (prerender)
    // so it survives into the SignalR circuit where HttpContext is null.
    private AuthenticationState? _cachedState;

    public WerkbankAuthStateProvider(
        ProtectedLocalStorage localStorage,
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider serviceProvider,
        ILogger<WerkbankAuthStateProvider> logger)
    {
        _localStorage = localStorage;
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            // Auth-Status aus DB prüfen
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Data.HostDbContext>();
            var settings = await dbContext.WerkbankSettings.AsNoTracking()
                .FirstOrDefaultAsync();

            if (settings == null || !settings.AuthEnabled)
            {
                // Auth deaktiviert: impliziter Admin
                var adminClaims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, Guid.Empty.ToString()),
                    new Claim(ClaimTypes.Name, "Admin"),
                    new Claim(ClaimTypes.Role, UserRole.Admin.ToString())
                };
                var adminState = new AuthenticationState(
                    new ClaimsPrincipal(new ClaimsIdentity(adminClaims, "NoAuth")));
                _cachedState = adminState;
                return adminState;
            }

            // During prerender: read JWT from HttpContext cookie (JS interop not available)
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                if (httpContext.Request.Cookies.TryGetValue(CookieName, out var cookieToken)
                    && !string.IsNullOrEmpty(cookieToken))
                {
                    var claims = ParseToken(cookieToken);
                    if (claims != null)
                    {
                        var state = new AuthenticationState(
                            new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt")));
                        _cachedState = state;
                        return state;
                    }
                }

                // HttpContext exists but no valid cookie — anonymous
                var anonState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                _cachedState = anonState;
                return anonState;
            }

            // No HttpContext (SignalR circuit) — try ProtectedLocalStorage
            try
            {
                var tokenResult = await _localStorage.GetAsync<string>(TokenKey);
                if (tokenResult.Success && !string.IsNullOrEmpty(tokenResult.Value))
                {
                    var claims = ParseToken(tokenResult.Value);
                    if (claims != null)
                    {
                        var state = new AuthenticationState(
                            new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt")));
                        _cachedState = state;
                        return state;
                    }

                    await _localStorage.DeleteAsync(TokenKey);
                }
            }
            catch (InvalidOperationException)
            {
                // JS interop not yet available — use cached state from prerender
                if (_cachedState != null)
                    return _cachedState;
            }

            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Fehler beim Abrufen des Auth-Status");

            // Use cached state if available
            if (_cachedState != null)
                return _cachedState;

            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    public async Task LoginAsync(string token)
    {
        await _localStorage.SetAsync(TokenKey, token);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task LogoutAsync()
    {
        await _localStorage.DeleteAsync(TokenKey);
        _cachedState = null;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task<string?> GetTokenAsync()
    {
        try
        {
            var result = await _localStorage.GetAsync<string>(TokenKey);
            return result.Success ? result.Value : null;
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<Claim>? ParseToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            if (jwt.ValidTo < DateTime.UtcNow)
                return null;

            // Map short JWT claim types to .NET ClaimTypes
            return jwt.Claims.Select(c => new Claim(
                c.Type switch
                {
                    "role" => ClaimTypes.Role,
                    "name" => ClaimTypes.Name,
                    "nameid" => ClaimTypes.NameIdentifier,
                    "email" => ClaimTypes.Email,
                    _ => c.Type
                },
                c.Value));
        }
        catch
        {
            return null;
        }
    }
}
