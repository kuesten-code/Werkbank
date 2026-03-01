using Kuestencode.Core.Interfaces;
using Kuestencode.Core.Models;
using Kuestencode.Shared.ApiClients;

namespace Kuestencode.Werkbank.Saldo.Services;

/// <summary>
/// ICustomerService-Implementierung via Host-API für Saldo im Microservice-Modus.
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
            var dtos = await _hostApiClient.GetAllCustomersAsync();
            return dtos.Select(MapToCustomer).ToList();
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
            var dto = await _hostApiClient.GetCustomerAsync(id);
            return dto != null ? MapToCustomer(dto) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching customer {CustomerId} from Host API", id);
            throw;
        }
    }

    public Task<Customer> CreateAsync(Customer customer)
        => throw new NotSupportedException("Not supported in microservice mode.");

    public Task UpdateAsync(Customer customer)
        => throw new NotSupportedException("Not supported in microservice mode.");

    public Task DeleteAsync(int id)
        => throw new NotSupportedException("Not supported in microservice mode.");

    public Task<string> GenerateCustomerNumberAsync()
        => throw new NotSupportedException("Not supported in microservice mode.");

    public Task<bool> CustomerNumberExistsAsync(string number)
        => throw new NotSupportedException("Not supported in microservice mode.");

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
