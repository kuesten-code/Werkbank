using Kuestencode.Werkbank.Host.Models.MobileRapport;

namespace Kuestencode.Werkbank.Host.Services;

public interface IMobileRapportService
{
    Task<bool> IsProjectSelectionAvailableAsync(Guid teamMemberId);
    Task<List<ProjectSelectDto>> GetProjectsAsync(Guid teamMemberId);
    Task<List<CustomerSelectDto>> GetCustomersAsync(Guid teamMemberId);
    Task<List<TimeEntryDto>> GetEntriesAsync(Guid teamMemberId, DateOnly date);
    Task<List<TimeEntryDto>> GetEntriesAsync(Guid teamMemberId, DateOnly from, DateOnly to);
    Task<TimeEntryDto> CreateEntryAsync(Guid teamMemberId, CreateTimeEntryDto dto);
    Task<TimeEntryDto> UpdateEntryAsync(Guid teamMemberId, int entryId, UpdateTimeEntryDto dto);
    Task DeleteEntryAsync(Guid teamMemberId, int entryId);
    Task<List<MobileTaskDto>> GetAssignedTasksAsync(Guid teamMemberId, bool includeCompleted = true);
    Task<MobileTaskDto> SetTaskCompletedAsync(Guid teamMemberId, Guid taskId, bool completed);
}
