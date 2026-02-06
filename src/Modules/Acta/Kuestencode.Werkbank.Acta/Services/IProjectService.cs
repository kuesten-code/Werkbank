using Kuestencode.Werkbank.Acta.Domain.Dtos;
using Kuestencode.Werkbank.Acta.Domain.Entities;
using Kuestencode.Werkbank.Acta.Domain.Enums;

namespace Kuestencode.Werkbank.Acta.Services;

public interface IProjectService
{
    Task<List<Project>> GetAllAsync(ProjectStatus? statusFilter = null, int? customerIdFilter = null);
    Task<Project?> GetByIdAsync(Guid id);
    Task<Project?> GetByProjectNumberAsync(string projectNumber);
    Task<Project> CreateAsync(CreateProjectDto dto);
    Task<Project> UpdateAsync(Guid id, UpdateProjectDto dto);
    Task DeleteAsync(Guid id);
    Task<bool> TransitionStatusAsync(Guid id, ProjectStatus targetStatus);
    Task<string> GenerateProjectNumberAsync();
}
