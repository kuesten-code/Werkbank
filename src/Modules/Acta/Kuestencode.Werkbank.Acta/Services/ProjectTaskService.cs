using Kuestencode.Werkbank.Acta.Data.Repositories;
using Kuestencode.Werkbank.Acta.Domain.Dtos;
using Kuestencode.Werkbank.Acta.Domain.Entities;
using Kuestencode.Werkbank.Acta.Domain.Enums;

namespace Kuestencode.Werkbank.Acta.Services;

public class ProjectTaskService : IProjectTaskService
{
    private readonly IProjectTaskRepository _taskRepository;

    public ProjectTaskService(IProjectTaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
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
        var sortOrder = dto.SortOrder > 0
            ? dto.SortOrder
            : await _taskRepository.GetNextSortOrderAsync(projectId);

        var task = new ProjectTask
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Title = dto.Title,
            Notes = dto.Notes,
            TargetDate = dto.TargetDate,
            AssignedUserId = dto.AssignedUserId,
            SortOrder = sortOrder,
            Status = ProjectTaskStatus.Open,
            CreatedAt = DateTime.UtcNow
        };

        return await _taskRepository.AddAsync(task);
    }

    public async Task<ProjectTask> UpdateAsync(Guid id, UpdateProjectTaskDto dto)
    {
        var task = await _taskRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException($"Aufgabe mit ID {id} nicht gefunden.");

        task.Title = dto.Title;
        task.Notes = dto.Notes;
        task.TargetDate = dto.TargetDate;
        task.AssignedUserId = dto.AssignedUserId;
        task.SortOrder = dto.SortOrder;

        // Status-Wechsel behandeln
        if (task.Status != dto.Status)
        {
            task.Status = dto.Status;
            if (dto.Status == ProjectTaskStatus.Completed)
                task.CompletedAt = DateTime.UtcNow;
            else
                task.CompletedAt = null;
        }

        return await _taskRepository.UpdateAsync(task);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _taskRepository.DeleteAsync(id);
    }

    public async Task<bool> ToggleStatusAsync(Guid id)
    {
        var task = await _taskRepository.GetByIdAsync(id);
        if (task == null)
            return false;

        if (task.Status == ProjectTaskStatus.Open)
        {
            task.Status = ProjectTaskStatus.Completed;
            task.CompletedAt = DateTime.UtcNow;
        }
        else
        {
            task.Status = ProjectTaskStatus.Open;
            task.CompletedAt = null;
        }

        await _taskRepository.UpdateAsync(task);
        return true;
    }

    public async Task UpdateSortOrdersAsync(IEnumerable<(Guid Id, int SortOrder)> updates)
    {
        await _taskRepository.UpdateSortOrdersAsync(updates);
    }
}
