using Kuestencode.Shared.Contracts.Host;
using Kuestencode.Shared.UI.Auth;
using Kuestencode.Werkbank.Saldo.Domain.Dtos;
using Kuestencode.Werkbank.Saldo.Services;
using Microsoft.AspNetCore.Mvc;

namespace Kuestencode.Werkbank.Saldo.Controllers;

[ApiController]
[Route("api/saldo/euer")]
[RequireRole(UserRole.Admin, UserRole.Buero)]
public class EuerController : ControllerBase
{
    private readonly IEuerService _euerService;
    private readonly IPdfReportService _pdfReportService;
    private readonly ILogger<EuerController> _logger;

    public EuerController(
        IEuerService euerService,
        IPdfReportService pdfReportService,
        ILogger<EuerController> logger)
    {
        _euerService = euerService;
        _pdfReportService = pdfReportService;
        _logger = logger;
    }

    /// <summary>
    /// Gibt die EÜR-Zusammenfassung für den angegebenen Zeitraum zurück.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<EuerSummaryDto>> GetEuer(
        [FromQuery] DateOnly? von = null,
        [FromQuery] DateOnly? bis = null)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var filter = new EuerFilterDto
        {
            Von = von ?? new DateOnly(today.Year, 1, 1),
            Bis = bis ?? new DateOnly(today.Year, 12, 31)
        };

        try
        {
            var summary = await _euerService.GetEuerSummaryAsync(filter);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Berechnen der EÜR für {Von} - {Bis}", filter.Von, filter.Bis);
            return StatusCode(500, "Fehler beim Berechnen der EÜR.");
        }
    }

    /// <summary>
    /// Generiert einen PDF-Bericht der EÜR für den angegebenen Zeitraum.
    /// </summary>
    [HttpGet("pdf")]
    public async Task<IActionResult> GetEuerPdf(
        [FromQuery] DateOnly? von = null,
        [FromQuery] DateOnly? bis = null)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var vonDate = von ?? new DateOnly(today.Year, 1, 1);
        var bisDate = bis ?? new DateOnly(today.Year, 12, 31);

        try
        {
            var pdfBytes = await _pdfReportService.GenerateEuerReportAsync(vonDate, bisDate);
            var fileName = $"EUER_{vonDate:yyyy-MM-dd}_{bisDate:yyyy-MM-dd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Generieren des EÜR-PDFs für {Von} - {Bis}", vonDate, bisDate);
            return StatusCode(500, "Fehler beim Generieren des EÜR-PDFs.");
        }
    }
}
