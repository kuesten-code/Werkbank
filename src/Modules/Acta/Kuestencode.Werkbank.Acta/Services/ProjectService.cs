using Kuestencode.Werkbank.Acta.Data.Repositories;
using Kuestencode.Werkbank.Acta.Domain.Dtos;
using Kuestencode.Werkbank.Acta.Domain.Entities;
using Kuestencode.Werkbank.Acta.Domain.Enums;
using Kuestencode.Werkbank.Acta.Domain.Services;

namespace Kuestencode.Werkbank.Acta.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly ProjectStatusService _statusService;

    public ProjectService(IProjectRepository projectRepository, ProjectStatusService statusService)
    {
        _projectRepository = projectRepository;
        _statusService = statusService;
    }

    public async Task<List<Project>> GetAllAsync(ProjectStatus? statusFilter = null, int? customerIdFilter = null)
    {
        return await _projectRepository.GetAllAsync(statusFilter, customerIdFilter);
    }

    public async Task<Project?> GetByIdAsync(Guid id)
    {
        return await _projectRepository.GetByIdAsync(id);
    }

    public async Task<Project?> GetByProjectNumberAsync(string projectNumber)
    {
        return await _projectRepository.GetByProjectNumberAsync(projectNumber);
    }

    public async Task<Project> CreateAsync(CreateProjectDto dto)
    {
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
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return await _projectRepository.AddAsync(project);
    }

    public async Task<Project> UpdateAsync(Guid id, UpdateProjectDto dto)
    {
        var project = await _projectRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException($"Projekt mit ID {id} nicht gefunden.");

        project.Name = dto.Name;
        project.Description = dto.Description;
        project.CustomerId = dto.CustomerId;
        project.Address = dto.Address;
        project.PostalCode = dto.PostalCode;
        project.City = dto.City;
        project.StartDate = dto.StartDate;
        project.TargetDate = dto.TargetDate;
        project.BudgetNet = dto.BudgetNet;
        project.UpdatedAt = DateTime.UtcNow;

        return await _projectRepository.UpdateAsync(project);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _projectRepository.DeleteAsync(id);
    }

    public async Task<bool> TransitionStatusAsync(Guid id, ProjectStatus targetStatus)
    {
        var project = await _projectRepository.GetByIdAsync(id);
        if (project == null)
            return false;

        if (!_statusService.CanTransitionTo(project, targetStatus))
            return false;

        _statusService.TransitionTo(project, targetStatus);
        project.UpdatedAt = DateTime.UtcNow;

        if (targetStatus == ProjectStatus.Completed)
            project.CompletedAt = DateTime.UtcNow;

        await _projectRepository.UpdateAsync(project);
        return true;
    }

    public async Task<string> GenerateProjectNumberAsync()
    {
        return await _projectRepository.GenerateProjectNumberAsync();
    }
}
