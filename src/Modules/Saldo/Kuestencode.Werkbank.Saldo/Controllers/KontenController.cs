using Kuestencode.Werkbank.Saldo.Domain.Dtos;
using Kuestencode.Werkbank.Saldo.Services;
using Microsoft.AspNetCore.Mvc;

namespace Kuestencode.Werkbank.Saldo.Controllers;

[ApiController]
[Route("api/saldo/konten")]
public class KontenController : ControllerBase
{
    private readonly IKontoService _kontoService;
    private readonly ILogger<KontenController> _logger;

    public KontenController(IKontoService kontoService, ILogger<KontenController> logger)
    {
        _kontoService = kontoService;
        _logger = logger;
    }

    /// <summary>
    /// Gibt alle Konten zurück, optional gefiltert nach Kontenrahmen.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<KontoDto>>> GetKonten([FromQuery] string? kontenrahmen = null)
    {
        var konten = await _kontoService.GetKontenAsync(kontenrahmen);
        return Ok(konten);
    }

    /// <summary>
    /// Gibt alle Kategorie-Konto-Mappings zurück.
    /// </summary>
    [HttpGet("mappings")]
    public async Task<ActionResult<List<KategorieKontoMappingDto>>> GetMappings([FromQuery] string? kontenrahmen = null)
    {
        var mappings = await _kontoService.GetMappingsAsync(kontenrahmen);
        return Ok(mappings);
    }

    /// <summary>
    /// Aktualisiert ein Kategorie-Konto-Mapping.
    /// </summary>
    [HttpPut("mappings/{id:guid}")]
    public async Task<ActionResult<KategorieKontoMappingDto>> UpdateMapping(
        Guid id,
        [FromBody] UpdateKategorieKontoMappingDto dto)
    {
        try
        {
            var result = await _kontoService.UpdateMappingAsync(id, dto);
            if (result == null) return NotFound();
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
