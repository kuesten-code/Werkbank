using System.Security.Claims;
using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.UI.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;

namespace Kuestencode.Rapport.Services;

public class UserContextService : IUserContextService
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHostApiClient _hostApiClient;
    private readonly JwtPrincipalValidator _validator;

    private ClaimsPrincipal? _cachedPrincipal;
    private (int? RolleId, string? RolleName)? _cachedRolle;

    public UserContextService(
        AuthenticationStateProvider authStateProvider,
        IHttpContextAccessor httpContextAccessor,
        IHostApiClient hostApiClient,
        JwtPrincipalValidator validator)
    {
        _authStateProvider = authStateProvider;
        _httpContextAccessor = httpContextAccessor;
        _hostApiClient = hostApiClient;
        _validator = validator;
    }

    private async Task<ClaimsPrincipal> GetPrincipalAsync()
    {
        if (_cachedPrincipal != null)
            return _cachedPrincipal;

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var token = WerkbankJwt.ExtractToken(httpContext);
            var p = token != null ? _validator.Validate(token) : null;
            if (p != null) { _cachedPrincipal = p; return p; }

            if (httpContext.User?.Identity?.IsAuthenticated == true)
            {
                _cachedPrincipal = httpContext.User;
                return httpContext.User;
            }
        }

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

    public async Task<(int? RolleId, string? RolleName)> GetCurrentUserMitarbeiterRolleAsync()
    {
        if (_cachedRolle.HasValue)
            return _cachedRolle.Value;

        var userId = await GetCurrentUserIdAsync();
        if (userId.HasValue)
        {
            var member = await _hostApiClient.GetTeamMemberAsync(userId.Value);
            if (member != null)
            {
                var result = (member.MitarbeiterRolleId, member.MitarbeiterRolleName);
                _cachedRolle = result;
                return result;
            }
        }

        return (null, null);
    }
}
