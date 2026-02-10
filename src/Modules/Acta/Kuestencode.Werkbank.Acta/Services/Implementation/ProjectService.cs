using Kuestencode.Werkbank.Acta.Data.Repositories;
using Kuestencode.Werkbank.Acta.Domain.Dtos;
using Kuestencode.Werkbank.Acta.Domain.Entities;
using Kuestencode.Werkbank.Acta.Domain.Enums;
using Kuestencode.Werkbank.Acta.Domain.Services;

namespace Kuestencode.Werkbank.Acta.Services;

/// <summary>
/// Service-Implementierung für Projektverwaltung.
/// </summary>
public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly ProjectStatusService _statusService;

    public ProjectService(
        IProjectRepository projectRepository,
        ProjectStatusService statusService)
    {
        _projectRepository = projectRepository;
        _statusService = statusService;
    }

    public async Task<List<Project>> GetAllAsync(ProjectFilterDto? filter = null)
    {
        return await _projectRepository.GetAllAsync(filter?.Status, filter?.CustomerId);
    }

    public async Task<Project?> GetByIdAsync(Guid id)
    {
        return await _projectRepository.GetByIdAsync(id);
    }

    public async Task<Project> CreateAsync(CreateProjectDto dto)
    {
        // Projektnummer prüfen
        if (string.IsNullOrWhiteSpace(dto.ProjectNumber))
        {
            throw new InvalidOperationException("Projektnummer ist erforderlich.");
        }

        if (await _projectRepository.ExistsNumberAsync(dto.ProjectNumber))
        {
            throw new InvalidOperationException($"Projektnummer '{dto.ProjectNumber}' existiert bereits.");
        }

        // Name prüfen
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new InvalidOperationException("Projektname ist erforderlich.");
        }

        // Customer prüfen
        if (dto.CustomerId <= 0)
        {
            throw new InvalidOperationException("Kunde muss ausgewählt werden.");
        }

        var nextExternalId = await _projectRepository.GetNextExternalIdAsync();

        var project = new Project
        {
            Id = Guid.NewGuid(),
            ProjectNumber = dto.ProjectNumber,
            Name = dto.Name,
            Description = dto.Description,
            CustomerId = dto.CustomerId,
            Address = dto.Address,
            PostalCode = dto.PostalCode,
            City = dto.City,
            StartDate = dto.StartDate,
            TargetDate = dto.TargetDate,
            BudgetNet = dto.BudgetNet,
            Status = ProjectStatus.Draft,
            ExternalId = nextExternalId
        };

        await _projectRepository.AddAsync(project);
        return project;
    }

    public async Task<Project> UpdateAsync(Guid id, UpdateProjectDto dto)
    {
        var project = await _projectRepository.GetByIdAsync(id);
        if (project == null)
        {
            throw new InvalidOperationException($"Projekt mit ID {id} nicht gefunden.");
        }

        if (!_statusService.KannBearbeitetWerden(project))
        {
            throw new InvalidOperationException(
                $"Projekt kann nicht bearbeitet werden. Status: {project.Status}");
        }

        // Name prüfen
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new InvalidOperationException("Projektname ist erforderlich.");
        }

        // Customer prüfen
        if (dto.CustomerId <= 0)
        {
            throw new InvalidOperationException("Kunde muss ausgewählt werden.");
        }

        // Eigenschaften aktualisieren
        project.Name = dto.Name;
        project.Description = dto.Description;
        project.CustomerId = dto.CustomerId;
        project.Address = dto.Address;
        project.PostalCode = dto.PostalCode;
        project.City = dto.City;
        project.StartDate = dto.StartDate;
        project.TargetDate = dto.TargetDate;
        project.BudgetNet = dto.BudgetNet;

        await _projectRepository.UpdateAsync(project);
        return project;
    }

    public async Task<Project> ChangeStatusAsync(Guid id, ProjectStatus newStatus)
    {
        var project = await _projectRepository.GetByIdAsync(id);
        if (project == null)
        {
            throw new InvalidOperationException($"Projekt mit ID {id} nicht gefunden.");
        }

        // State Machine für den Übergang verwenden
        _statusService.TransitionTo(project, newStatus);

        await _projectRepository.UpdateAsync(project);
        return project;
    }

    public async Task DeleteAsync(Guid id)
    {
        await _projectRepository.DeleteAsync(id);
    }

    public async Task<bool> ProjectNumberExistsAsync(string projectNumber)
    {
        return await _projectRepository.ExistsNumberAsync(projectNumber);
    }

    public IReadOnlyList<(ProjectStatus TargetStatus, string ActionName)> GetAvailableTransitions(Project project)
    {
        return _statusService.GetVerfuegbareUebergaenge(project);
    }

    public async Task<string> GenerateProjectNumberAsync()
    {
        return await _projectRepository.GenerateProjectNumberAsync();
    }

    public async Task EnsureExternalIdsAsync()
    {
        var projects = await _projectRepository.GetAllAsync();
        var withoutExternalId = projects.Where(p => !p.ExternalId.HasValue).ToList();

        if (withoutExternalId.Count == 0)
            return;

        var nextId = await _projectRepository.GetNextExternalIdAsync();
        foreach (var project in withoutExternalId)
        {
            project.ExternalId = nextId++;
            await _projectRepository.UpdateAsync(project);
        }
    }
}

