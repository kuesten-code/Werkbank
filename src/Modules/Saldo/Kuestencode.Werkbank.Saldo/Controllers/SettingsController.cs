using Kuestencode.Shared.Contracts.Host;
using Kuestencode.Shared.UI.Auth;
using Kuestencode.Werkbank.Saldo.Domain.Dtos;
using Kuestencode.Werkbank.Saldo.Services;
using Microsoft.AspNetCore.Mvc;

namespace Kuestencode.Werkbank.Saldo.Controllers;

[ApiController]
[Route("api/saldo/settings")]
[RequireRole(UserRole.Admin)]
public class SettingsController : ControllerBase
{
    private readonly ISaldoSettingsService _settingsService;
    private readonly IKontoService _kontoService;
    private readonly IKontoMappingService _mappingService;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        ISaldoSettingsService settingsService,
        IKontoService kontoService,
        IKontoMappingService mappingService,
        ILogger<SettingsController> logger)
    {
        _settingsService = settingsService;
        _kontoService = kontoService;
        _mappingService = mappingService;
        _logger = logger;
    }

    /// <summary>
    /// Gibt die aktuellen Saldo-Einstellungen zurück (Kontenrahmen, DATEV-Nummern).
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

    /// <summary>
    /// Gibt alle Konten für den angegebenen Kontenrahmen zurück.
    /// </summary>
    [HttpGet("konten")]
    public async Task<ActionResult<List<KontoDto>>> GetKonten([FromQuery] string rahmen = "SKR03")
    {
        var konten = await _kontoService.GetKontenAsync(rahmen);
        return Ok(konten);
    }

    /// <summary>
    /// Gibt das aktuelle Kategorie-Konto-Mapping zurück.
    /// Enthält Standard-Mappings, IsCustom=true markiert manuell überschriebene Einträge.
    /// </summary>
    [HttpGet("mapping")]
    public async Task<ActionResult<List<KategorieKontoMappingDto>>> GetMapping([FromQuery] string? kontenrahmen = null)
    {
        var mappings = await _kontoService.GetMappingsAsync(kontenrahmen);
        return Ok(mappings);
    }

    /// <summary>
    /// Aktualisiert das Konto-Mapping für eine einzelne Kategorie (via Mapping-ID).
    /// Setzt IsCustom = true.
    /// </summary>
    [HttpPut("mapping/{id:guid}")]
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
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Aktualisieren des Mappings {Id}", id);
            return StatusCode(500, "Fehler beim Speichern des Mappings.");
        }
    }

    /// <summary>
    /// Gibt alle benutzerdefinierten Mapping-Overrides zurück.
    /// Overrides haben Vorrang vor Standard-Mappings bei der Buchungsgenerierung.
    /// </summary>
    [HttpGet("overrides")]
    public async Task<ActionResult<List<KontoMappingOverrideDto>>> GetOverrides()
    {
        var overrides = await _mappingService.GetOverridesAsync();
        return Ok(overrides);
    }

    /// <summary>
    /// Setzt oder aktualisiert einen Override für eine Kategorie.
    /// Beispiel: Alle "Material"-Belege sollen auf Konto 3400 statt Standard 3300.
    /// </summary>
    [HttpPut("overrides/{kategorie}")]
    public async Task<ActionResult<KontoMappingOverrideDto>> SetOverride(
        string kategorie,
        [FromBody] SetKontoMappingOverrideDto dto)
    {
        try
        {
            var result = await _mappingService.UpdateMappingAsync(kategorie, dto.KontoNummer);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Setzen des Overrides für {Kategorie}", kategorie);
            return StatusCode(500, "Fehler beim Speichern des Overrides.");
        }
    }

    /// <summary>
    /// Entfernt einen Override – das Standard-Mapping wird wieder aktiv.
    /// </summary>
    [HttpDelete("overrides/{kategorie}")]
    public async Task<IActionResult> DeleteOverride(string kategorie)
    {
        await _mappingService.ResetMappingAsync(kategorie);
        return NoContent();
    }

    /// <summary>
    /// Gibt das vollständig aufgelöste Mapping zurück (Override wenn aktiv, sonst Standard).
    /// Nützlich für das UI: zeigt für jede Kategorie das tatsächlich verwendete Konto.
    /// </summary>
    [HttpGet("mapping/resolved")]
    public async Task<ActionResult<List<ResolvedKontoMappingDto>>> GetResolvedMapping(
        [FromQuery] string kontenrahmen = "SKR03")
    {
        var resolved = await _mappingService.GetResolvedMappingsAsync(kontenrahmen);
        return Ok(resolved);
    }
}
