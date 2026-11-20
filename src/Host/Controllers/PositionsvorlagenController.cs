using Kuestencode.Shared.Contracts.Host;
using Kuestencode.Werkbank.Host.Auth;
using Kuestencode.Werkbank.Host.Services.Positionskatalog;
using Microsoft.AspNetCore.Mvc;

namespace Kuestencode.Werkbank.Host.Controllers;

[ApiController]
[Route("api/positionsvorlagen")]
[RequireRole(UserRole.Admin, UserRole.Buero)]
public class PositionsvorlagenController : ControllerBase
{
    private const int MaxLimit = 50;

    private readonly IPositionskatalogService _katalog;

    public PositionsvorlagenController(IPositionskatalogService katalog)
    {
        _katalog = katalog;
    }

    [HttpGet]
    public async Task<ActionResult<List<PositionsvorlageDto>>> Suchen([FromQuery] string? suche, [FromQuery] int limit = 10)
    {
        var treffer = await _katalog.SucheAsync(suche, Math.Clamp(limit, 1, MaxLimit));
        return Ok(treffer.Select(v => new PositionsvorlageDto
        {
            Id = v.Id,
            Text = v.Text,
            Einheit = v.Einheit,
            Einzelpreis = v.Einzelpreis
        }).ToList());
    }

    [HttpGet("einheiten")]
    public async Task<ActionResult<List<string>>> Einheiten()
    {
        return Ok(await _katalog.GetEinheitenAsync());
    }

    [HttpPost("merken")]
    public async Task<IActionResult> Merken([FromBody] List<PositionsvorlageMerkenDto> positionen)
    {
        await _katalog.MerkenAsync(positionen);
        return NoContent();
    }
}
