using Kuestencode.Werkbank.Acta.Data.Repositories;
using Kuestencode.Werkbank.Acta.Domain.Dtos;
using Kuestencode.Werkbank.Acta.Domain.Entities;
using Kuestencode.Werkbank.Acta.Domain.Enums;
using Kuestencode.Werkbank.Acta.Domain.Services;

namespace Kuestencode.Werkbank.Acta.Services;

/// <summary>
/// Service-Implementierung für Projektaufgaben.
/// </summary>
public class ProjectTaskService : IProjectTaskService
{
    private readonly IProjectTaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ProjectStatusService _statusService;

    public ProjectTaskService(
        IProjectTaskRepository taskRepository,
        IProjectRepository projectRepository,
        ProjectStatusService statusService)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _statusService = statusService;
    }

    public async Task<List<ProjectTask>> GetByProjectIdAsync(Guid projectId)
    {
        return await _taskRepository.GetByProjectIdAsync(projectId);
    }

    public async Task<ProjectTask?> GetByIdAsync(Guid id)
    {
        return await _taskRepository.GetByIdAsync(id);
    }

    public async Task<ProjectTask> CreateAsync(Guid projectId, CreateProjectTaskDto dto)
    {
        var project = await _projectRepository.GetByIdAsync(projectId);
        if (project == null)
        {
            throw new InvalidOperationException($"Projekt mit ID {projectId} nicht gefunden.");
        }

        if (!_statusService.KannBearbeitetWerden(project))
        {
            throw new InvalidOperationException(
                $"Aufgaben können nicht hinzugefügt werden. Projektstatus: {project.Status}");
        }

        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            throw new InvalidOperationException("Titel ist erforderlich.");
        }

        var nextSortOrder = await _taskRepository.GetNextSortOrderAsync(projectId);

        var task = new ProjectTask
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Title = dto.Title,
            Notes = dto.Notes,
            TargetDate = dto.TargetDate,
            AssignedUserId = dto.AssignedUserId,
            Status = ProjectTaskStatus.Open,
            SortOrder = nextSortOrder
        };

        await _taskRepository.AddAsync(task);
        return task;
    }

    public async Task<ProjectTask> UpdateAsync(Guid id, UpdateProjectTaskDto dto)
    {
        var task = await _taskRepository.GetByIdAsync(id);
        if (task == null)
        {
            throw new InvalidOperationException($"Aufgabe mit ID {id} nicht gefunden.");
        }

        var project = await _projectRepository.GetByIdAsync(task.ProjectId);
        if (project != null && !_statusService.KannBearbeitetWerden(project))
        {
            throw new InvalidOperationException(
                $"Aufgaben können nicht bearbeitet werden. Projektstatus: {project.Status}");
        }

        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            throw new InvalidOperationException("Titel ist erforderlich.");
        }

        task.Title = dto.Title;
        task.Notes = dto.Notes;
        task.TargetDate = dto.TargetDate;
        task.AssignedUserId = dto.AssignedUserId;

        await _taskRepository.UpdateAsync(task);
        return task;
    }

    public async Task<ProjectTask> SetCompletedAsync(Guid id)
    {
        var task = await _taskRepository.GetByIdAsync(id);
        if (task == null)
        {
            throw new InvalidOperationException($"Aufgabe mit ID {id} nicht gefunden.");
        }

        if (task.Status == ProjectTaskStatus.Completed)
        {
            return task; // Bereits erledigt
        }

        task.Status = ProjectTaskStatus.Completed;
        task.CompletedAt = DateTime.UtcNow;

        await _taskRepository.UpdateAsync(task);
        return task;
    }

    public async Task<ProjectTask> SetOpenAsync(Guid id)
    {
        var task = await _taskRepository.GetByIdAsync(id);
        if (task == null)
        {
            throw new InvalidOperationException($"Aufgabe mit ID {id} nicht gefunden.");
        }

        if (task.Status == ProjectTaskStatus.Open)
        {
            return task; // Bereits offen
        }

        task.Status = ProjectTaskStatus.Open;
        task.CompletedAt = null;

        await _taskRepository.UpdateAsync(task);
        return task;
    }

    public async Task ReorderAsync(Guid projectId, List<Guid> taskIds)
    {
        var project = await _projectRepository.GetByIdAsync(projectId);
        if (project == null)
        {
            throw new InvalidOperationException($"Projekt mit ID {projectId} nicht gefunden.");
        }

        if (!_statusService.KannBearbeitetWerden(project))
        {
            throw new InvalidOperationException(
                $"Aufgaben können nicht umsortiert werden. Projektstatus: {project.Status}");
        }

        var tasks = await _taskRepository.GetByProjectIdAsync(projectId);
        var taskDict = tasks.ToDictionary(t => t.Id);

        var tasksToUpdate = new List<ProjectTask>();
        for (int i = 0; i < taskIds.Count; i++)
        {
            if (taskDict.TryGetValue(taskIds[i], out var task))
            {
                task.SortOrder = i;
                tasksToUpdate.Add(task);
            }
        }

        if (tasksToUpdate.Count > 0)
        {
            await _taskRepository.UpdateRangeAsync(tasksToUpdate);
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        var task = await _taskRepository.GetByIdAsync(id);
        if (task == null)
        {
            throw new InvalidOperationException($"Aufgabe mit ID {id} nicht gefunden.");
        }

        var project = await _projectRepository.GetByIdAsync(task.ProjectId);
        if (project != null && !_statusService.KannBearbeitetWerden(project))
        {
            throw new InvalidOperationException(
                $"Aufgaben können nicht gelöscht werden. Projektstatus: {project.Status}");
        }

        await _taskRepository.DeleteAsync(id);
    }
}
