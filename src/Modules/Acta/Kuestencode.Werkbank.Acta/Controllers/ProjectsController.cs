using Kuestencode.Werkbank.Acta.Controllers.Dtos;
using Kuestencode.Werkbank.Acta.Domain.Dtos;
using Kuestencode.Werkbank.Acta.Domain.Entities;
using Kuestencode.Werkbank.Acta.Domain.Enums;
using Kuestencode.Werkbank.Acta.Services;
using Microsoft.AspNetCore.Mvc;

namespace Kuestencode.Werkbank.Acta.Controllers;

[ApiController]
[Route("api/acta/projects")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(IProjectService projectService, ILogger<ProjectsController> logger)
    {
        _projectService = projectService;
        _logger = logger;
    }

    /// <summary>
    /// Lädt alle Projekte mit optionalem Filter.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ProjectDto>>> GetAll(
        [FromQuery] string? status = null,
        [FromQuery] int? customerId = null)
    {
        var filter = new ProjectFilterDto();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<ProjectStatus>(status, true, out var parsedStatus))
        {
            filter.Status = parsedStatus;
        }

        filter.CustomerId = customerId;

        var projects = await _projectService.GetAllAsync(filter);
        return Ok(projects.Select(MapToDto).ToList());
    }

    /// <summary>
    /// Lädt ein Projekt anhand der ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectDto>> GetById(Guid id)
    {
        var project = await _projectService.GetByIdAsync(id);
        if (project == null)
        {
            return NotFound();
        }

        return Ok(MapToDto(project));
    }

    /// <summary>
    /// Erstellt ein neues Projekt.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create([FromBody] CreateProjectRequest request)
    {
        try
        {
            var dto = new CreateProjectDto
            {
                ProjectNumber = request.ProjectNumber,
                Name = request.Name,
                Description = request.Description,
                CustomerId = request.CustomerId,
                Address = request.Address,
                PostalCode = request.PostalCode,
                City = request.City,
                StartDate = request.StartDate,
                TargetDate = request.TargetDate,
                BudgetNet = request.BudgetNet
            };

            var project = await _projectService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = project.Id }, MapToDto(project));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Aktualisiert ein Projekt.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProjectDto>> Update(Guid id, [FromBody] UpdateProjectRequest request)
    {
        try
        {
            var dto = new UpdateProjectDto
            {
                Name = request.Name,
                Description = request.Description,
                CustomerId = request.CustomerId,
                Address = request.Address,
                PostalCode = request.PostalCode,
                City = request.City,
                StartDate = request.StartDate,
                TargetDate = request.TargetDate,
                BudgetNet = request.BudgetNet
            };

            var project = await _projectService.UpdateAsync(id, dto);
            return Ok(MapToDto(project));
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("nicht gefunden"))
            {
                return NotFound(new { error = ex.Message });
            }
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Ändert den Status eines Projekts.
    /// </summary>
    [HttpPost("{id:guid}/status")]
    public async Task<ActionResult<ProjectDto>> ChangeStatus(Guid id, [FromBody] ChangeStatusRequest request)
    {
        try
        {
            if (!Enum.TryParse<ProjectStatus>(request.NewStatus, true, out var newStatus))
            {
                return BadRequest(new { error = $"Ungültiger Status: {request.NewStatus}" });
            }

            var project = await _projectService.ChangeStatusAsync(id, newStatus);
            return Ok(MapToDto(project));
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("nicht gefunden"))
            {
                return NotFound(new { error = ex.Message });
            }
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Löscht ein Projekt (nur bei Status Draft).
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _projectService.DeleteAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("nicht gefunden"))
            {
                return NotFound(new { error = ex.Message });
            }
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Lädt eine Projektzusammenfassung mit Daten aus Rapport und Faktura.
    /// </summary>
    [HttpGet("{id:guid}/summary")]
    public async Task<ActionResult<ProjectSummaryDto>> GetSummary(Guid id)
    {
        var project = await _projectService.GetByIdAsync(id);
        if (project == null)
        {
            return NotFound();
        }

        // TODO: Daten aus Rapport und Faktura laden, wenn API-Clients verfügbar
        var summary = new ProjectSummaryDto
        {
            ProjectId = project.Id,
            ProjectNumber = project.ProjectNumber,
            ProjectName = project.Name,
            BudgetNet = project.BudgetNet,
            // Platzhalter - werden später durch echte API-Aufrufe ersetzt
            TotalHours = 0,
            TotalLaborCost = 0,
            TotalInvoicedNet = 0,
            InvoiceCount = 0
        };

        return Ok(summary);
    }

    private ProjectDto MapToDto(Project project)
    {
        var transitions = _projectService.GetAvailableTransitions(project);

        return new ProjectDto
        {
            Id = project.Id,
            ProjectNumber = project.ProjectNumber,
            Name = project.Name,
            Description = project.Description,
            CustomerId = project.CustomerId,
            Address = project.Address,
            PostalCode = project.PostalCode,
            City = project.City,
            Status = project.Status.ToString(),
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            StartDate = project.StartDate,
            TargetDate = project.TargetDate,
            CompletedAt = project.CompletedAt,
            BudgetNet = project.BudgetNet,
            OpenTasksCount = project.OpenTasksCount,
            CompletedTasksCount = project.CompletedTasksCount,
            ProgressPercent = project.ProgressPercent,
            AvailableTransitions = transitions.Select(t => new StatusTransitionDto
            {
                TargetStatus = t.TargetStatus.ToString(),
                ActionName = t.ActionName
            }).ToList()
        };
    }
}
