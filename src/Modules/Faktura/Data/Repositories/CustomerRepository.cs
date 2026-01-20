using Kuestencode.Faktura.Models;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Faktura.Data.Repositories;

public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Customer?> GetByCustomerNumberAsync(string customerNumber)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.CustomerNumber == customerNumber);
    }

    public async Task<bool> CustomerNumberExistsAsync(string customerNumber)
    {
        return await _dbSet
            .AnyAsync(c => c.CustomerNumber == customerNumber);
    }

    public async Task<string> GenerateCustomerNumberAsync()
    {
        var lastCustomer = await _dbSet
            .OrderByDescending(c => c.CustomerNumber)
            .FirstOrDefaultAsync();

        if (lastCustomer == null)
        {
            return "K00001";
        }

        // Extract number from last customer number (e.g., "K00001" -> "00001")
        var numberPart = lastCustomer.CustomerNumber.Substring(1);

        if (int.TryParse(numberPart, out int lastNumber))
        {
            var nextNumber = lastNumber + 1;
            return $"K{nextNumber:D5}";
        }

        // Fallback if parsing fails
        return "K00001";
    }

    public async Task<IEnumerable<Customer>> SearchAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await GetAllAsync();
        }

        var lowerSearchTerm = searchTerm.ToLower();

        return await _dbSet
            .Where(c =>
                c.CustomerNumber.ToLower().Contains(lowerSearchTerm) ||
                c.Name.ToLower().Contains(lowerSearchTerm) ||
                (c.Email != null && c.Email.ToLower().Contains(lowerSearchTerm)) ||
                c.City.ToLower().Contains(lowerSearchTerm))
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public override async Task<IEnumerable<Customer>> GetAllAsync()
    {
        return await _dbSet
            .OrderBy(c => c.Name)
            .ToListAsync();
    }
}
