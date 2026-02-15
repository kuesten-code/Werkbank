using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.EntityFrameworkCore;
using Kuestencode.Werkbank.Host.Models;

namespace Kuestencode.Werkbank.Host.Auth;

public class WerkbankAuthStateProvider : AuthenticationStateProvider
{
    private readonly ProtectedLocalStorage _localStorage;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WerkbankAuthStateProvider> _logger;

    private const string TokenKey = "werkbank_auth_token";

    public WerkbankAuthStateProvider(
        ProtectedLocalStorage localStorage,
        IServiceProvider serviceProvider,
        ILogger<WerkbankAuthStateProvider> logger)
    {
        _localStorage = localStorage;
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
                return new AuthenticationState(
                    new ClaimsPrincipal(new ClaimsIdentity(adminClaims, "NoAuth")));
            }

            // Token aus LocalStorage lesen
            var tokenResult = await _localStorage.GetAsync<string>(TokenKey);
            if (!tokenResult.Success || string.IsNullOrEmpty(tokenResult.Value))
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            var claims = ParseToken(tokenResult.Value);
            if (claims == null)
            {
                await _localStorage.DeleteAsync(TokenKey);
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            return new AuthenticationState(
                new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt")));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Fehler beim Abrufen des Auth-Status");
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

            return jwt.Claims;
        }
        catch
        {
            return null;
        }
    }
}
