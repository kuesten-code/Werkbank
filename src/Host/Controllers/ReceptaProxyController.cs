using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.Contracts.Recepta;
using Kuestencode.Werkbank.Host.Auth;
using Kuestencode.Werkbank.Host.Models;
using Microsoft.AspNetCore.Mvc;

namespace Kuestencode.Werkbank.Host.Controllers;

[ApiController]
[Route("api/recepta")]
[RequireRole(UserRole.Admin, UserRole.Buero)]
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
    public async Task<ActionResult<List<ReceptaDocumentDto>>> GetDocumentsByProject(
        Guid projectId,
        [FromQuery] bool onlyUnattached = false)
    {
        var documents = await _receptaApiClient.GetDocumentsByProjectAsync(projectId, onlyUnattached);
        return Ok(documents);
    }

    [HttpPost("documents/mark-attached")]
    public async Task<IActionResult> MarkDocumentsAttached([FromBody] MarkDocumentsAttachedRequestDto request)
    {
        var success = await _receptaApiClient.MarkDocumentsAsAttachedAsync(request.DocumentIds);
        if (!success)
        {
            return BadRequest();
        }

        return Ok();
    }
}
