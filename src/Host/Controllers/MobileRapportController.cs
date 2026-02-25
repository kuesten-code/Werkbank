using System.Security.Claims;
using Kuestencode.Werkbank.Host.Models.MobileRapport;
using Kuestencode.Werkbank.Host.Services;
using Microsoft.AspNetCore.Mvc;

namespace Kuestencode.Werkbank.Host.Controllers;

[ApiController]
[Route("api/mobile/rapport")]
public class MobileRapportController : ControllerBase
{
    private readonly IMobileRapportService _mobileRapportService;
    private readonly ILogger<MobileRapportController> _logger;

    public MobileRapportController(
        IMobileRapportService mobileRapportService,
        ILogger<MobileRapportController> logger)
    {
        _mobileRapportService = mobileRapportService;
        _logger = logger;
    }

    [HttpGet("projects")]
    public async Task<IActionResult> GetProjects()
    {
        var teamMemberId = GetCurrentTeamMemberId();
        if (teamMemberId == Guid.Empty)
            return Unauthorized();

        try
        {
            var projects = await _mobileRapportService.GetProjectsAsync(teamMemberId);
            return Ok(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting projects for team member {TeamMemberId}", teamMemberId);
            return StatusCode(500, new { error = "Fehler beim Laden der Projekte" });
        }
    }

    [HttpGet("entries")]
    public async Task<IActionResult> GetEntries(
        [FromQuery] string? date = null,
        [FromQuery] string? from = null,
        [FromQuery] string? to = null)
    {
        var teamMemberId = GetCurrentTeamMemberId();
        if (teamMemberId == Guid.Empty)
            return Unauthorized();

        try
        {
            List<TimeEntryDto> entries;

            if (!string.IsNullOrEmpty(date) && DateOnly.TryParse(date, out var singleDate))
            {
                entries = await _mobileRapportService.GetEntriesAsync(teamMemberId, singleDate);
            }
            else if (!string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(to) &&
                     DateOnly.TryParse(from, out var fromDate) &&
                     DateOnly.TryParse(to, out var toDate))
            {
                entries = await _mobileRapportService.GetEntriesAsync(teamMemberId, fromDate, toDate);
            }
            else
            {
                entries = await _mobileRapportService.GetEntriesAsync(teamMemberId, DateOnly.FromDateTime(DateTime.Today));
            }

            return Ok(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entries for team member {TeamMemberId}", teamMemberId);
            return StatusCode(500, new { error = "Fehler beim Laden der Einträge" });
        }
    }

    [HttpPost("entries")]
    public async Task<IActionResult> CreateEntry([FromBody] CreateTimeEntryDto dto)
    {
        var teamMemberId = GetCurrentTeamMemberId();
        if (teamMemberId == Guid.Empty)
            return Unauthorized();

        if (!dto.StartTime.HasValue || !dto.EndTime.HasValue)
            return BadRequest(new { error = "Bitte Start- und Endzeit auswählen" });
        if (dto.EndTime <= dto.StartTime)
            return BadRequest(new { error = "Endzeit muss nach der Startzeit liegen" });
        if (!dto.ProjectId.HasValue && !dto.CustomerId.HasValue)
            return BadRequest(new { error = "Bitte Kunde oder Projekt auswählen" });

        try
        {
            var entry = await _mobileRapportService.CreateEntryAsync(teamMemberId, dto);
            return Ok(entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating entry for team member {TeamMemberId}", teamMemberId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut("entries/{id:int}")]
    public async Task<IActionResult> UpdateEntry(int id, [FromBody] UpdateTimeEntryDto dto)
    {
        var teamMemberId = GetCurrentTeamMemberId();
        if (teamMemberId == Guid.Empty)
            return Unauthorized();

        if (!dto.StartTime.HasValue || !dto.EndTime.HasValue)
            return BadRequest(new { error = "Bitte Start- und Endzeit auswählen" });
        if (dto.EndTime <= dto.StartTime)
            return BadRequest(new { error = "Endzeit muss nach der Startzeit liegen" });
        if (!dto.ProjectId.HasValue && !dto.CustomerId.HasValue)
            return BadRequest(new { error = "Bitte Kunde oder Projekt auswählen" });

        try
        {
            var entry = await _mobileRapportService.UpdateEntryAsync(teamMemberId, id, dto);
            return Ok(entry);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating entry {EntryId} for team member {TeamMemberId}", id, teamMemberId);
            return StatusCode(500, new { error = "Fehler beim Aktualisieren" });
        }
    }

    [HttpDelete("entries/{id:int}")]
    public async Task<IActionResult> DeleteEntry(int id)
    {
        var teamMemberId = GetCurrentTeamMemberId();
        if (teamMemberId == Guid.Empty)
            return Unauthorized();

        try
        {
            await _mobileRapportService.DeleteEntryAsync(teamMemberId, id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting entry {EntryId} for team member {TeamMemberId}", id, teamMemberId);
            return StatusCode(500, new { error = "Fehler beim Löschen" });
        }
    }

    private Guid GetCurrentTeamMemberId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return Guid.Empty;
    }
}
