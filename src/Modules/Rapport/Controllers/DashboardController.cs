using Kuestencode.Rapport.Services;
using Kuestencode.Shared.Contracts.Rapport;
using Microsoft.AspNetCore.Mvc;

namespace Kuestencode.Rapport.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly DashboardService _dashboardService;
    private readonly SettingsService _settingsService;
    private readonly TimeRoundingService _roundingService;

    public DashboardController(
        DashboardService dashboardService,
        SettingsService settingsService,
        TimeRoundingService roundingService)
    {
        _dashboardService = dashboardService;
        _settingsService = settingsService;
        _roundingService = roundingService;
    }

    [HttpGet("projects/{projectId:int}/hours")]
    public async Task<ActionResult<ProjectHoursResponseDto>> GetProjectHours(int projectId)
    {
        var entries = await _dashboardService.GetEntriesAsync(
            DateTime.MinValue,
            DateTime.UtcNow,
            customerIds: null,
            projectIds: new[] { projectId });

        var settings = await _settingsService.GetSettingsAsync();
        var roundingMinutes = settings.RoundingMinutes;

        var totalHours = 0m;
        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            var duration = (entry.EndTime ?? now) - entry.StartTime;
            if (roundingMinutes > 0)
            {
                duration = _roundingService.RoundDuration(duration, roundingMinutes);
            }
            totalHours += (decimal)duration.TotalHours;
        }

        return Ok(new ProjectHoursResponseDto
        {
            ProjectId = projectId,
            TotalHours = Math.Round(totalHours, 2)
        });
    }
}
