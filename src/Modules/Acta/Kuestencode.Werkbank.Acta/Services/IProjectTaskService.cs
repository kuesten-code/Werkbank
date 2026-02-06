using Kuestencode.Werkbank.Acta.Domain.Dtos;
using Kuestencode.Werkbank.Acta.Domain.Entities;
using Kuestencode.Werkbank.Acta.Domain.Enums;

namespace Kuestencode.Werkbank.Acta.Services;

public interface IProjectTaskService
{
    Task<List<ProjectTask>> GetByProjectIdAsync(Guid projectId);
    Task<ProjectTask?> GetByIdAsync(Guid id);
    Task<ProjectTask> CreateAsync(Guid projectId, CreateProjectTaskDto dto);
    Task<ProjectTask> UpdateAsync(Guid id, UpdateProjectTaskDto dto);
    Task DeleteAsync(Guid id);
    Task<bool> ToggleStatusAsync(Guid id);
    Task UpdateSortOrdersAsync(IEnumerable<(Guid Id, int SortOrder)> updates);
}
