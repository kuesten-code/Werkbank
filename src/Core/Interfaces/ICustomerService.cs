using Kuestencode.Core.Models;

namespace Kuestencode.Core.Interfaces;

/// <summary>
/// Service interface for customer data operations.
/// </summary>
public interface ICustomerService
{
    /// <summary>
    /// Gets all customers.
    /// </summary>
    Task<List<Customer>> GetAllAsync();

    /// <summary>
    /// Gets a customer by ID.
    /// </summary>
    Task<Customer?> GetByIdAsync(int id);

    /// <summary>
    /// Creates a new customer.
    /// </summary>
    Task<Customer> CreateAsync(Customer customer);

    /// <summary>
    /// Updates an existing customer.
    /// </summary>
    Task UpdateAsync(Customer customer);

    /// <summary>
    /// Deletes a customer.
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// Generates a new customer number.
    /// </summary>
    Task<string> GenerateCustomerNumberAsync();

    /// <summary>
    /// Checks if a customer number already exists.
    /// </summary>
    Task<bool> CustomerNumberExistsAsync(string number);
}
