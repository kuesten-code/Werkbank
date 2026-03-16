using Kuestencode.Shared.Contracts.Host;
using Kuestencode.Shared.UI.Auth;
using Kuestencode.Werkbank.Saldo.Domain.Dtos;
using Kuestencode.Werkbank.Saldo.Services;
using Microsoft.AspNetCore.Mvc;

namespace Kuestencode.Werkbank.Saldo.Controllers;

[ApiController]
[Route("api/saldo")]
[RequireRole(UserRole.Admin, UserRole.Buero)]
public class SaldoController : ControllerBase
{
    private readonly ISaldoAggregationService _saldoService;
    private readonly IEinnahmenService _einnahmenService;
    private readonly IAusgabenService _ausgabenService;
    private readonly ILogger<SaldoController> _logger;

    public SaldoController(
        ISaldoAggregationService saldoService,
        IEinnahmenService einnahmenService,
        IAusgabenService ausgabenService,
        ILogger<SaldoController> logger)
    {
        _saldoService = saldoService;
        _einnahmenService = einnahmenService;
        _ausgabenService = ausgabenService;
        _logger = logger;
    }

    /// <summary>
    /// Gibt die Saldo-Übersicht (Einnahmen, Ausgaben, Saldo, USt-Zahllast) für einen Zeitraum zurück.
    /// Zufluss-/Abflussprinzip: maßgeblich ist das Zahlungsdatum (PaidDate).
    /// </summary>
    [HttpGet("uebersicht")]
    public async Task<ActionResult<SaldoUebersichtDto>> GetUebersicht(
        [FromQuery] DateOnly? von = null,
        [FromQuery] DateOnly? bis = null)
    {
        var (vonDate, bisDate) = GetDateRange(von, bis);
        try
        {
            var result = await _saldoService.GetUebersichtAsync(vonDate, bisDate);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Laden der Saldo-Übersicht für {Von} - {Bis}", vonDate, bisDate);
            return StatusCode(500, "Fehler beim Laden der Saldo-Übersicht.");
        }
    }

    /// <summary>
    /// Gibt alle Buchungen (Einnahmen + Ausgaben) für einen Zeitraum zurück, sortiert nach Zahlungsdatum.
    /// </summary>
    [HttpGet("buchungen")]
    public async Task<ActionResult<List<BuchungDto>>> GetAlleBuchungen(
        [FromQuery] DateOnly? von = null,
        [FromQuery] DateOnly? bis = null)
    {
        var (vonDate, bisDate) = GetDateRange(von, bis);
        try
        {
            var result = await _saldoService.GetAlleBuchungenAsync(vonDate, bisDate);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Laden der Buchungen für {Von} - {Bis}", vonDate, bisDate);
            return StatusCode(500, "Fehler beim Laden der Buchungen.");
        }
    }

    /// <summary>
    /// Gibt alle Einnahmen (bezahlte Faktura-Rechnungen) für einen Zeitraum zurück.
    /// </summary>
    [HttpGet("einnahmen")]
    public async Task<ActionResult<List<BuchungDto>>> GetEinnahmen(
        [FromQuery] DateOnly? von = null,
        [FromQuery] DateOnly? bis = null)
    {
        var (vonDate, bisDate) = GetDateRange(von, bis);
        try
        {
            var result = await _einnahmenService.GetEinnahmenAsync(vonDate, bisDate);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Laden der Einnahmen für {Von} - {Bis}", vonDate, bisDate);
            return StatusCode(500, "Fehler beim Laden der Einnahmen.");
        }
    }

    /// <summary>
    /// Gibt alle Ausgaben (bezahlte Recepta-Belege) für einen Zeitraum zurück.
    /// </summary>
    [HttpGet("ausgaben")]
    public async Task<ActionResult<List<BuchungDto>>> GetAusgaben(
        [FromQuery] DateOnly? von = null,
        [FromQuery] DateOnly? bis = null)
    {
        var (vonDate, bisDate) = GetDateRange(von, bis);
        try
        {
            var result = await _ausgabenService.GetAusgabenAsync(vonDate, bisDate);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Laden der Ausgaben für {Von} - {Bis}", vonDate, bisDate);
            return StatusCode(500, "Fehler beim Laden der Ausgaben.");
        }
    }

    /// <summary>
    /// Gibt die monatsweise USt-Übersicht (Umsatzsteuer vs. Vorsteuer) für ein Jahr zurück.
    /// Nützlich für die monatliche USt-Voranmeldung.
    /// </summary>
    [HttpGet("ust")]
    public async Task<ActionResult<UstUebersichtDto>> GetUstUebersicht([FromQuery] int? jahr = null)
    {
        var year = jahr ?? DateTime.Today.Year;
        var von = new DateOnly(year, 1, 1);
        var bis = new DateOnly(year, 12, 31);
        try
        {
            var result = await _saldoService.GetUstUebersichtAsync(von, bis);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Laden der USt-Übersicht für {Jahr}", year);
            return StatusCode(500, "Fehler beim Laden der USt-Übersicht.");
        }
    }

    private static (DateOnly Von, DateOnly Bis) GetDateRange(DateOnly? von, DateOnly? bis)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return (
            von ?? new DateOnly(today.Year, 1, 1),
            bis ?? new DateOnly(today.Year, 12, 31)
        );
    }
}
