using Kuestencode.Shared.Contracts.Host;
using Kuestencode.Shared.UI.Auth;
using Kuestencode.Werkbank.Acta.Domain.Entities;
using Kuestencode.Werkbank.Acta.Services;
using Microsoft.AspNetCore.Mvc;

namespace Kuestencode.Werkbank.Acta.Controllers;

[ApiController]
[Route("api/acta/pinned-projects")]
[RequireRole(UserRole.Admin, UserRole.Buero, UserRole.Mitarbeiter)]
public class PinnedProjectsController : ControllerBase
{
    private readonly IPinnedProjectService _pinnedProjectService;

    public PinnedProjectsController(IPinnedProjectService pinnedProjectService)
    {
        _pinnedProjectService = pinnedProjectService;
    }

    [HttpGet]
    public async Task<ActionResult<List<PinnedProjectDto>>> GetPinned()
    {
        var projects = await _pinnedProjectService.GetPinnedProjectsAsync();
        return Ok(projects.Select(MapToDto).ToList());
    }

    [HttpGet("pinnable")]
    public async Task<ActionResult<List<PinnedProjectDto>>> GetPinnable()
    {
        var projects = await _pinnedProjectService.GetPinnableActiveProjectsAsync();
        return Ok(projects.Select(MapToDto).ToList());
    }

    [HttpPost("{projectId:guid}")]
    public async Task<IActionResult> Pin(Guid projectId)
    {
        await _pinnedProjectService.PinAsync(projectId);
        return NoContent();
    }

    [HttpDelete("{projectId:guid}")]
    public async Task<IActionResult> Unpin(Guid projectId)
    {
        await _pinnedProjectService.UnpinAsync(projectId);
        return NoContent();
    }

    private static PinnedProjectDto MapToDto(Project project) => new()
    {
        Id = project.Id,
        ProjectNumber = project.ProjectNumber,
        Name = project.Name,
        CustomerId = project.CustomerId,
        TargetDate = project.TargetDate,
        BudgetNet = project.BudgetNet
    };
}

public class PinnedProjectDto
{
    public Guid Id { get; set; }
    public string ProjectNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public DateOnly? TargetDate { get; set; }
    public decimal? BudgetNet { get; set; }
}
