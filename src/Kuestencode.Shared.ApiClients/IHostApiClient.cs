using Kuestencode.Shared.Contracts.Acta;
using Kuestencode.Shared.Contracts.Host;
using Kuestencode.Shared.Contracts.Navigation;
using Kuestencode.Shared.Contracts.Rapport;

namespace Kuestencode.Shared.ApiClients;

public interface IHostApiClient
{
    Task<CompanyDto?> GetCompanyAsync();
    Task UpdateCompanyAsync(UpdateCompanyRequest request);
    Task<CustomerDto?> GetCustomerAsync(int customerId);
    Task<List<CustomerDto>> GetAllCustomersAsync();
    Task<List<NavItemDto>> GetNavigationAsync();
    Task<List<TeamMemberDto>> GetTeamMembersAsync();
    Task<ProjectHoursResponseDto?> GetProjectHoursAsync(int projectId);
    Task<List<ActaProjectDto>> GetActaProjectsAsync();
    Task<ActaProjectDto?> GetActaProjectAsync(int externalId);
}
