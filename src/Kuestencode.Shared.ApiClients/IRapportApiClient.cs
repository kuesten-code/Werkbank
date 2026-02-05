using Kuestencode.Shared.Contracts.Rapport;

namespace Kuestencode.Shared.ApiClients;

public interface IRapportApiClient
{
    Task<bool> IsHealthyAsync();
    Task<byte[]> GenerateTimesheetPdfAsync(TimesheetExportRequestDto request);
    Task<byte[]> GenerateTimesheetCsvAsync(TimesheetExportRequestDto request);
    Task<ProjectHoursResponseDto?> GetProjectHoursAsync(int projectId);
}
