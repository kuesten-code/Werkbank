using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Kuestencode.Rapport.Services;

public class UserContextService : IUserContextService
{
    private readonly AuthenticationStateProvider _authStateProvider;

    public UserContextService(AuthenticationStateProvider authStateProvider)
    {
        _authStateProvider = authStateProvider;
    }

    public async Task<Guid?> GetCurrentUserIdAsync()
    {
        var state = await _authStateProvider.GetAuthenticationStateAsync();
        var idClaim = state.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(idClaim, out var id) && id != Guid.Empty)
            return id;
        return null;
    }

    public async Task<string?> GetCurrentUserNameAsync()
    {
        var state = await _authStateProvider.GetAuthenticationStateAsync();
        return state.User.FindFirstValue(ClaimTypes.Name);
    }

    public async Task<string?> GetCurrentUserRoleAsync()
    {
        var state = await _authStateProvider.GetAuthenticationStateAsync();
        return state.User.FindFirstValue(ClaimTypes.Role);
    }

    public async Task<bool> IsAdminOrBueroAsync()
    {
        var role = await GetCurrentUserRoleAsync();
        return role == "Admin" || role == "Buero";
    }
}
