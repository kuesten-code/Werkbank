using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.Contracts.Acta;
using Kuestencode.Werkbank.Host.Auth;
using Kuestencode.Werkbank.Host.Models;
using Microsoft.AspNetCore.Mvc;

namespace Kuestencode.Werkbank.Host.Controllers;

[ApiController]
[Route("api/acta-proxy")]
[RequireRole(UserRole.Admin, UserRole.Buero)]
public class ActaProxyController : ControllerBase
{
    private readonly IActaApiClient _actaApiClient;

    public ActaProxyController(IActaApiClient actaApiClient)
    {
        _actaApiClient = actaApiClient;
    }

    [HttpGet("projects")]
    public async Task<ActionResult<List<ActaProjectDto>>> GetProjects()
    {
        var projects = await _actaApiClient.GetProjectsAsync();
        return Ok(projects);
    }

    [HttpGet("projects/{externalId:int}")]
    public async Task<ActionResult<ActaProjectDto>> GetProject(int externalId)
    {
        var project = await _actaApiClient.GetProjectByExternalIdAsync(externalId);
        if (project == null)
            return NotFound();

        return Ok(project);
    }
}
