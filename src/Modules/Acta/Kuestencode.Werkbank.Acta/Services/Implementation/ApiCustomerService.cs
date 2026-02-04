using Kuestencode.Core.Interfaces;
using Kuestencode.Core.Models;
using Kuestencode.Shared.ApiClients;

namespace Kuestencode.Werkbank.Acta.Services;

/// <summary>
/// Implementation of ICustomerService that communicates with Host via HTTP API.
/// Used when Acta runs as a standalone microservice.
/// </summary>
public class ApiCustomerService : ICustomerService
{
    private readonly IHostApiClient _hostApiClient;
    private readonly ILogger<ApiCustomerService> _logger;

    public ApiCustomerService(IHostApiClient hostApiClient, ILogger<ApiCustomerService> logger)
    {
        _hostApiClient = hostApiClient;
        _logger = logger;
    }

    public async Task<List<Customer>> GetAllAsync()
    {
        try
        {
            var customerDtos = await _hostApiClient.GetAllCustomersAsync();
            return customerDtos.Select(MapToCustomer).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching customers from Host API");
            throw;
        }
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        try
        {
            var customerDto = await _hostApiClient.GetCustomerAsync(id);
            return customerDto != null ? MapToCustomer(customerDto) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching customer {CustomerId} from Host API", id);
            throw;
        }
    }

    public Task<Customer> CreateAsync(Customer customer)
    {
        throw new NotSupportedException("Creating customers is not supported in microservice mode. Use Host API directly.");
    }

    public Task UpdateAsync(Customer customer)
    {
        throw new NotSupportedException("Updating customers is not supported in microservice mode. Use Host API directly.");
    }

    public Task DeleteAsync(int id)
    {
        throw new NotSupportedException("Deleting customers is not supported in microservice mode. Use Host API directly.");
    }

    public Task<string> GenerateCustomerNumberAsync()
    {
        throw new NotSupportedException("Generating customer numbers is not supported in microservice mode. Use Host API directly.");
    }

    public Task<bool> CustomerNumberExistsAsync(string number)
    {
        throw new NotSupportedException("Checking customer numbers is not supported in microservice mode. Use Host API directly.");
    }

    private static Customer MapToCustomer(Kuestencode.Shared.Contracts.Host.CustomerDto dto)
    {
        return new Customer
        {
            Id = dto.Id,
            CustomerNumber = dto.CustomerNumber,
            Name = dto.Name,
            Address = dto.Address,
            PostalCode = dto.PostalCode,
            City = dto.City,
            Country = dto.Country,
            Email = dto.Email,
            Phone = dto.Phone,
            Notes = dto.Notes,
            Salutation = dto.Salutation
        };
    }
}
