using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.Contracts.Recepta;
using Microsoft.AspNetCore.Mvc;

namespace Kuestencode.Werkbank.Host.Controllers;

[ApiController]
[Route("api/recepta")]
public class ReceptaProxyController : ControllerBase
{
    private readonly IReceptaApiClient _receptaApiClient;

    public ReceptaProxyController(IReceptaApiClient receptaApiClient)
    {
        _receptaApiClient = receptaApiClient;
    }

    [HttpGet("documents/project/{projectId:guid}/expenses")]
    public async Task<ActionResult<ProjectExpensesResponseDto>> GetProjectExpenses(Guid projectId)
    {
        var result = await _receptaApiClient.GetProjectExpensesAsync(projectId);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet("documents/project/{projectId:guid}")]
    public async Task<ActionResult<List<ReceptaDocumentDto>>> GetDocumentsByProject(Guid projectId)
    {
        var documents = await _receptaApiClient.GetDocumentsByProjectAsync(projectId);
        return Ok(documents);
    }
}
