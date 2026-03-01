using Kuestencode.Werkbank.Saldo.Domain.Dtos;
using Kuestencode.Werkbank.Saldo.Services;
using Microsoft.AspNetCore.Mvc;

namespace Kuestencode.Werkbank.Saldo.Controllers;

[ApiController]
[Route("api/saldo/settings")]
public class SettingsController : ControllerBase
{
    private readonly ISaldoSettingsService _settingsService;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(ISaldoSettingsService settingsService, ILogger<SettingsController> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    /// <summary>
    /// Gibt die aktuellen Saldo-Einstellungen zurück.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<SaldoSettingsDto>> GetSettings()
    {
        var settings = await _settingsService.GetSettingsAsync();
        if (settings == null) return NotFound("Keine Einstellungen gefunden.");
        return Ok(settings);
    }

    /// <summary>
    /// Aktualisiert die Saldo-Einstellungen.
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<SaldoSettingsDto>> UpdateSettings([FromBody] UpdateSaldoSettingsDto dto)
    {
        try
        {
            var result = await _settingsService.UpdateSettingsAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Aktualisieren der Saldo-Einstellungen");
            return StatusCode(500, "Fehler beim Speichern der Einstellungen.");
        }
    }
}
