using System.Security.Claims;
using Kuestencode.Shared.Contracts.Host;

namespace Kuestencode.Shared.UI.Services;

public static class UserRoleResolver
{
    public static UserRole ResolveRole(ClaimsPrincipal? user, UserRole fallbackRole = UserRole.Mitarbeiter)
    {
        if (user?.Identity?.AuthenticationType == "NoAuth")
        {
            return UserRole.Admin;
        }

        var roleClaim =
            user?.FindFirstValue(ClaimTypes.Role) ??
            user?.FindFirstValue("role") ??
            user?.Claims.FirstOrDefault(c => c.Type.EndsWith("/role", StringComparison.OrdinalIgnoreCase))?.Value;

        return TryParseRole(roleClaim, out var parsedRole) ? parsedRole : fallbackRole;
    }

    public static bool TryParseRole(string? roleClaim, out UserRole role)
    {
        if (!string.IsNullOrWhiteSpace(roleClaim) &&
            Enum.TryParse<UserRole>(roleClaim, ignoreCase: true, out var enumRole))
        {
            role = enumRole;
            return true;
        }

        if (int.TryParse(roleClaim, out var intRole) &&
            Enum.IsDefined(typeof(UserRole), intRole))
        {
            role = (UserRole)intRole;
            return true;
        }

        role = default;
        return false;
    }
}
