using Kuestencode.Shared.Contracts.Host;
using Kuestencode.Shared.Contracts.Navigation;

namespace Kuestencode.Shared.ApiClients;

public interface IHostApiClient
{
    Task<CompanyDto?> GetCompanyAsync();
    Task UpdateCompanyAsync(UpdateCompanyRequest request);
    Task<CustomerDto?> GetCustomerAsync(int customerId);
    Task<List<CustomerDto>> GetAllCustomersAsync();
    Task<List<NavItemDto>> GetNavigationAsync();
    Task<List<TeamMemberDto>> GetTeamMembersAsync();
}
