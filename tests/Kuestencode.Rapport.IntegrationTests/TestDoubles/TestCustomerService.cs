using Kuestencode.Core.Interfaces;
using Kuestencode.Core.Models;

namespace Kuestencode.Rapport.IntegrationTests.TestDoubles;

public sealed class TestCustomerService : ICustomerService
{
    private readonly List<Customer> _customers = new();
    private int _nextId = 1;

    public IReadOnlyList<Customer> Customers => _customers;

    public Task<List<Customer>> GetAllAsync()
    {
        return Task.FromResult(_customers.ToList());
    }

    public Task<Customer?> GetByIdAsync(int id)
    {
        return Task.FromResult(_customers.FirstOrDefault(c => c.Id == id));
    }

    public Task<Customer> CreateAsync(Customer customer)
    {
        customer.Id = _nextId++;
        _customers.Add(customer);
        return Task.FromResult(customer);
    }

    public Task UpdateAsync(Customer customer)
    {
        var existing = _customers.FirstOrDefault(c => c.Id == customer.Id);
        if (existing != null)
        {
            existing.Name = customer.Name;
            existing.CustomerNumber = customer.CustomerNumber;
            existing.Address = customer.Address;
            existing.PostalCode = customer.PostalCode;
            existing.City = customer.City;
            existing.Country = customer.Country;
            existing.Email = customer.Email;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(int id)
    {
        var existing = _customers.FirstOrDefault(c => c.Id == id);
        if (existing != null)
        {
            _customers.Remove(existing);
        }
        return Task.CompletedTask;
    }

    public Task<string> GenerateCustomerNumberAsync()
    {
        return Task.FromResult($"C-{_nextId:0000}");
    }

    public Task<bool> CustomerNumberExistsAsync(string number)
    {
        return Task.FromResult(_customers.Any(c => string.Equals(c.CustomerNumber, number, StringComparison.OrdinalIgnoreCase)));
    }
}
