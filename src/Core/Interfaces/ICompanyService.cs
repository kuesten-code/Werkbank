using Kuestencode.Core.Models;

namespace Kuestencode.Core.Interfaces;

/// <summary>
/// Service interface for company/business data operations.
/// </summary>
public interface ICompanyService
{
    /// <summary>
    /// Gets the company information. Creates a default company if none exists.
    /// </summary>
    Task<Company> GetCompanyAsync();

    /// <summary>
    /// Updates the company information.
    /// </summary>
    Task<Company> UpdateCompanyAsync(Company company);

    /// <summary>
    /// Checks if essential company data is filled.
    /// </summary>
    Task<bool> HasCompanyDataAsync();

    /// <summary>
    /// Checks if email (SMTP) is properly configured.
    /// </summary>
    Task<bool> IsEmailConfiguredAsync();
}
