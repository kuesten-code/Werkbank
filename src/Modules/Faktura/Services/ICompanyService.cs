using Kuestencode.Faktura.Models;

namespace Kuestencode.Faktura.Services;

public interface ICompanyService
{
    Task<Company> GetCompanyAsync();
    Task<Company> UpdateCompanyAsync(Company company);
    Task<bool> HasCompanyDataAsync();
    Task<bool> IsEmailConfiguredAsync();
}
