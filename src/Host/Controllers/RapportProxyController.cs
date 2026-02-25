using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.Contracts.Rapport;
using Kuestencode.Werkbank.Host.Auth;
using Kuestencode.Werkbank.Host.Models;
using Microsoft.AspNetCore.Mvc;

namespace Kuestencode.Werkbank.Host.Controllers;

[ApiController]
[Route("api/rapport")]
[RequireRole(UserRole.Admin, UserRole.Buero)]
public class RapportProxyController : ControllerBase
{
    private readonly IRapportApiClient _rapportApiClient;

    public RapportProxyController(IRapportApiClient rapportApiClient)
    {
        _rapportApiClient = rapportApiClient;
    }

    [HttpGet("projects/{projectId:int}/hours")]
    public async Task<ActionResult<ProjectHoursResponseDto>> GetProjectHours(int projectId)
    {
        var result = await _rapportApiClient.GetProjectHoursAsync(projectId);
        if (result == null)
            return NotFound();

        return Ok(result);
    }
}
