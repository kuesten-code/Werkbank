using Kuestencode.Shared.Contracts.Host;
using Kuestencode.Shared.Contracts.Locatio;
using Kuestencode.Shared.UI.Auth;
using Kuestencode.Werkbank.Locatio.Services;
using Microsoft.AspNetCore.Mvc;

namespace Kuestencode.Werkbank.Locatio.Controllers;

/// <summary>
/// Liefert Saldo die Buchungen der Vermietung (service-to-service, Zufluss-/Abflussprinzip).
/// </summary>
[ApiController]
[Route("api/locatio/saldo-buchungen")]
[RequireRole(UserRole.Admin, UserRole.Buero)]
public class SaldoBuchungenController : ControllerBase
{
    private readonly ISaldoBuchungService _buchungen;

    public SaldoBuchungenController(ISaldoBuchungService buchungen)
    {
        _buchungen = buchungen;
    }

    [HttpGet]
    public async Task<ActionResult<List<LocatioBuchungDto>>> Get([FromQuery] DateOnly from, [FromQuery] DateOnly to)
    {
        if (from > to)
            return BadRequest(new { error = "'from' liegt nach 'to'." });

        return Ok(await _buchungen.GetBuchungenAsync(from, to));
    }
}
