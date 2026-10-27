using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.Contracts.Host;
using Kuestencode.Shared.UI.Auth;
using Kuestencode.Werkbank.Locatio.Data;
using Kuestencode.Werkbank.Locatio.Domain.Dtos;
using Kuestencode.Werkbank.Locatio.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Locatio.Controllers;

/// <summary>
/// Downloads für die Oberfläche: Dateianhänge und der Veräußerungsgewinn-Export.
/// </summary>
[ApiController]
[Route("api/locatio")]
[RequireRole(UserRole.Admin, UserRole.Buero)]
public class DateienController : ControllerBase
{
    private readonly IDbContextFactory<LocatioDbContext> _dbFactory;
    private readonly IVeraeusserungsgewinnService _gewinn;
    private readonly IHostApiClient _host;
    private readonly ILogger<DateienController> _logger;

    public DateienController(
        IDbContextFactory<LocatioDbContext> dbFactory,
        IVeraeusserungsgewinnService gewinn,
        IHostApiClient host,
        ILogger<DateienController> logger)
    {
        _dbFactory = dbFactory;
        _gewinn = gewinn;
        _host = host;
        _logger = logger;
    }

    [HttpGet("dokumente/{id:guid}")]
    public async Task<IActionResult> Dokument(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var dokument = await db.Dokumente.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
        if (dokument == null)
            return NotFound();

        return File(dokument.Inhalt, dokument.ContentType, dokument.Dateiname);
    }

    [HttpGet("veraeusserungsgewinn/{art}/{id:guid}.{format}")]
    public async Task<IActionResult> Veraeusserungsgewinn(string art, Guid id, string format)
    {
        var dto = art switch
        {
            "einheit" => await _gewinn.FuerEinheitAsync(id),
            "gebaeude" => await _gewinn.FuerGebaeudeAsync(id),
            _ => null
        };
        if (dto == null)
            return NotFound();

        var dateiname = $"Veraeusserungsgewinn_{Dateiname(dto)}";
        return format switch
        {
            "csv" => File(VeraeusserungsgewinnExport.ErzeugeCsv(dto), "text/csv", $"{dateiname}.csv"),
            "pdf" => File(VeraeusserungsgewinnExport.ErzeugePdf(dto, await EigentuemerAsync()), "application/pdf", $"{dateiname}.pdf"),
            _ => NotFound()
        };
    }

    private static string Dateiname(VeraeusserungsgewinnDto dto) =>
        string.Concat(dto.Objekt.Select(c => char.IsLetterOrDigit(c) ? c : '_'));

    private async Task<string> EigentuemerAsync()
    {
        try
        {
            var firma = await _host.GetCompanyAsync();
            if (firma == null)
                return string.Empty;
            return string.IsNullOrWhiteSpace(firma.BusinessName) ? firma.OwnerFullName : firma.BusinessName;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Firmendaten für den Veräußerungsgewinn-Export nicht verfügbar");
            return string.Empty;
        }
    }
}
