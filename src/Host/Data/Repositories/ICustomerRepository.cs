using Kuestencode.Core.Models;

namespace Kuestencode.Werkbank.Host.Data.Repositories;

/// <summary>
/// Repository-Interface für Customer-spezifische Operationen.
/// </summary>
public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByCustomerNumberAsync(string customerNumber);
    Task<bool> CustomerNumberExistsAsync(string customerNumber);
    Task<string> GenerateCustomerNumberAsync();
    Task<IEnumerable<Customer>> SearchAsync(string searchTerm);
}
