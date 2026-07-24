using Microsoft.AspNetCore.Mvc;
using Kuestencode.Core.Enums;
using Kuestencode.Core.Models;
using Kuestencode.Shared.Contracts.Host;
using Kuestencode.Werkbank.Host.Auth;
using Kuestencode.Werkbank.Host.Services;

namespace Kuestencode.Werkbank.Host.Controllers;

[ApiController]
[Route("api/number-format-settings")]
[RequireRole(UserRole.Admin)]
public class NumberFormatSettingsController : ControllerBase
{
    private readonly INumberFormatSettingsService _service;

    public NumberFormatSettingsController(INumberFormatSettingsService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<NumberFormatSettingsDto>> GetSettings()
    {
        var settings = await _service.GetSettingsAsync();
        return Ok(MapToDto(settings));
    }

    [HttpPut]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateNumberFormatSettingsRequest request)
    {
        var settings = await _service.GetSettingsAsync();

        settings.InvoiceFormat = request.InvoiceFormat;
        settings.QuoteFormat = request.QuoteFormat;
        settings.ProjectFormat = request.ProjectFormat;
        settings.IncomingInvoiceFormat = request.IncomingInvoiceFormat;
        settings.CreditNoteFormat = request.CreditNoteFormat;

        await _service.UpdateSettingsAsync(settings);
        return NoContent();
    }

    private static NumberFormatSettingsDto MapToDto(NumberFormatSettings settings)
    {
        return new NumberFormatSettingsDto
        {
            InvoiceFormat = settings.InvoiceFormat,
            QuoteFormat = settings.QuoteFormat,
            ProjectFormat = settings.ProjectFormat,
            IncomingInvoiceFormat = settings.IncomingInvoiceFormat,
            CreditNoteFormat = settings.CreditNoteFormat
        };
    }
}
