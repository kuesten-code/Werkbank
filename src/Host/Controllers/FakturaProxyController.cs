using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.Contracts.Faktura;
using Kuestencode.Werkbank.Host.Auth;
using Kuestencode.Werkbank.Host.Models;
using Microsoft.AspNetCore.Mvc;

namespace Kuestencode.Werkbank.Host.Controllers;

[ApiController]
[Route("api/faktura-proxy")]
[RequireRole(UserRole.Admin, UserRole.Buero)]
public class FakturaProxyController : ControllerBase
{
    private readonly IFakturaApiClient _fakturaApiClient;

    public FakturaProxyController(IFakturaApiClient fakturaApiClient)
    {
        _fakturaApiClient = fakturaApiClient;
    }

    [HttpGet("invoices/project/{projectId:int}")]
    public async Task<ActionResult<ProjectInvoicesResponseDto>> GetProjectInvoices(int projectId)
    {
        var result = await _fakturaApiClient.GetProjectInvoicesAsync(projectId);
        if (result == null)
            return NotFound();

        return Ok(result);
    }
}
