using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.Contracts.Acta;
using Microsoft.AspNetCore.Mvc;

namespace Kuestencode.Werkbank.Host.Controllers;

[ApiController]
[Route("api/acta-proxy")]
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
