using Kuestencode.Werkbank.Recepta.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Recepta.Data.Repositories;

/// <summary>
/// Repository-Implementierung für Lieferanten.
/// </summary>
public class SupplierRepository : ISupplierRepository
{
    private readonly ReceptaDbContext _context;

    public SupplierRepository(ReceptaDbContext context)
    {
        _context = context;
    }

    public async Task<Supplier?> GetByIdAsync(Guid id)
    {
        return await _context.Suppliers
            .Include(s => s.Documents)
            .Include(s => s.OcrPatterns)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Supplier?> GetByNumberAsync(string supplierNumber)
    {
        return await _context.Suppliers
            .Include(s => s.Documents)
            .FirstOrDefaultAsync(s => s.SupplierNumber == supplierNumber);
    }

    public async Task<List<Supplier>> GetAllAsync(string? search = null)
    {
        var query = _context.Suppliers
            .Include(s => s.Documents)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(s =>
                s.Name.ToLower().Contains(term) ||
                s.SupplierNumber.ToLower().Contains(term) ||
                (s.City != null && s.City.ToLower().Contains(term)));
        }

        return await query
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<Supplier?> FindByNameAsync(string name)
    {
        var lower = name.ToLower();

        // Erst exakte Suche
        var exact = await _context.Suppliers
            .Include(s => s.Documents)
            .FirstOrDefaultAsync(s => s.Name.ToLower() == lower);

        if (exact != null)
            return exact;

        // Dann Teilsuche (Name im Suchtext enthalten oder Suchtext im Namen)
        return await _context.Suppliers
            .Include(s => s.Documents)
            .FirstOrDefaultAsync(s =>
                s.Name.ToLower().Contains(lower) ||
                lower.Contains(s.Name.ToLower()));
    }

    public async Task<Supplier?> FindByTaxIdAsync(string taxId)
    {
        if (string.IsNullOrWhiteSpace(taxId)) return null;

        var normalized = taxId.Replace(" ", "").ToUpper();
        return await _context.Suppliers
            .Include(s => s.Documents)
            .FirstOrDefaultAsync(s => s.TaxId != null
                && s.TaxId.Replace(" ", "").ToUpper() == normalized);
    }

    public async Task<Supplier?> FindByIbanAsync(string iban)
    {
        if (string.IsNullOrWhiteSpace(iban)) return null;

        var normalized = iban.Replace(" ", "").ToUpper();
        return await _context.Suppliers
            .Include(s => s.Documents)
            .FirstOrDefaultAsync(s => s.Iban != null
                && s.Iban.Replace(" ", "").ToUpper() == normalized);
    }

    public async Task AddAsync(Supplier supplier)
    {
        await _context.Suppliers.AddAsync(supplier);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Supplier supplier)
    {
        _context.Suppliers.Update(supplier);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var supplier = await _context.Suppliers
            .Include(s => s.Documents)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (supplier == null)
        {
            throw new InvalidOperationException($"Lieferant mit ID {id} nicht gefunden.");
        }

        if (supplier.Documents.Count > 0)
        {
            throw new InvalidOperationException(
                "Lieferant kann nicht gelöscht werden. Es gibt noch zugehörige Belege.");
        }

        _context.Suppliers.Remove(supplier);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsNumberAsync(string supplierNumber)
    {
        return await _context.Suppliers.AnyAsync(s => s.SupplierNumber == supplierNumber);
    }

    public async Task<string> GenerateSupplierNumberAsync()
    {
        var prefix = "L-";

        var lastNumber = await _context.Suppliers
            .Where(s => s.SupplierNumber.StartsWith(prefix))
            .OrderByDescending(s => s.SupplierNumber)
            .Select(s => s.SupplierNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastNumber != null)
        {
            var numberPart = lastNumber[prefix.Length..];
            if (int.TryParse(numberPart, out var num))
                nextNumber = num + 1;
        }

        return $"{prefix}{nextNumber:D4}";
    }
}
