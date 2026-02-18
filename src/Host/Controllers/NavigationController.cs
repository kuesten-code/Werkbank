using Kuestencode.Shared.Contracts.Host;
using Kuestencode.Shared.Contracts.Navigation;
using Kuestencode.Shared.UI.Services;
using Kuestencode.Werkbank.Host.Services;
using Microsoft.AspNetCore.Mvc;

namespace Kuestencode.Werkbank.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NavigationController : ControllerBase
{
    private readonly IHostNavigationService _navigationService;

    public NavigationController(IHostNavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    [HttpGet]
    public ActionResult<List<NavItemDto>> GetNavigationItems()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            // Anonymous fallback: do not leak full navigation.
            return Ok(_navigationService.GetVisibleNavigationItems(UserRole.Mitarbeiter, authEnabled: true));
        }

        var authEnabled = !string.Equals(User.Identity?.AuthenticationType, "NoAuth", StringComparison.Ordinal);
        var currentRole = UserRoleResolver.ResolveRole(User, UserRole.Mitarbeiter);

        return Ok(_navigationService.GetVisibleNavigationItems(currentRole, authEnabled));
    }
}
