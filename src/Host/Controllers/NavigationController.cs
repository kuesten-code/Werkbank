using Kuestencode.Shared.Contracts.Navigation;
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
        return Ok(_navigationService.GetNavigationItems());
    }
}
