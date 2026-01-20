using Kuestencode.Faktura.Models;

namespace Kuestencode.Faktura.Data.Repositories;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByCustomerNumberAsync(string customerNumber);
    Task<bool> CustomerNumberExistsAsync(string customerNumber);
    Task<string> GenerateCustomerNumberAsync();
    Task<IEnumerable<Customer>> SearchAsync(string searchTerm);
}
