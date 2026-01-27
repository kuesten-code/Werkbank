namespace Kuestencode.Shared.ApiClients;

public interface IRapportApiClient
{
    Task<bool> IsHealthyAsync();
    Task<byte[]> GenerateTimesheetPdfAsync(Kuestencode.Shared.Contracts.Rapport.TimesheetExportRequestDto request);
    Task<byte[]> GenerateTimesheetCsvAsync(Kuestencode.Shared.Contracts.Rapport.TimesheetExportRequestDto request);
}
