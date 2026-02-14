using Kuestencode.Werkbank.Recepta.Controllers.Dtos;
using Kuestencode.Werkbank.Recepta.Data.Repositories;
using Kuestencode.Werkbank.Recepta.Domain.Dtos;
using Kuestencode.Werkbank.Recepta.Domain.Entities;

namespace Kuestencode.Werkbank.Recepta.Services;

/// <summary>
/// Service-Implementierung für Lieferantenverwaltung.
/// </summary>
public class SupplierService : ISupplierService
{
    private readonly ISupplierRepository _supplierRepository;

    public SupplierService(ISupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    public async Task<IEnumerable<SupplierDto>> GetAllAsync(string? search = null)
    {
        var suppliers = await _supplierRepository.GetAllAsync(search);
        return suppliers.Select(MapToDto);
    }

    public async Task<SupplierDto?> GetByIdAsync(Guid id)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id);
        return supplier != null ? MapToDto(supplier) : null;
    }

    public async Task<SupplierDto> CreateAsync(CreateSupplierDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.SupplierNumber))
        {
            throw new InvalidOperationException("Lieferantennummer ist erforderlich.");
        }

        if (await _supplierRepository.ExistsNumberAsync(dto.SupplierNumber))
        {
            throw new InvalidOperationException($"Lieferantennummer '{dto.SupplierNumber}' existiert bereits.");
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new InvalidOperationException("Lieferantenname ist erforderlich.");
        }

        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            SupplierNumber = dto.SupplierNumber,
            Name = dto.Name,
            Address = dto.Address,
            PostalCode = dto.PostalCode,
            City = dto.City,
            Country = dto.Country,
            Email = dto.Email,
            Phone = dto.Phone,
            TaxId = dto.TaxId,
            Iban = dto.Iban,
            Bic = dto.Bic,
            Notes = dto.Notes
        };

        await _supplierRepository.AddAsync(supplier);
        return MapToDto(supplier);
    }

    public async Task<SupplierDto> UpdateAsync(Guid id, UpdateSupplierDto dto)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id);
        if (supplier == null)
        {
            throw new InvalidOperationException($"Lieferant mit ID {id} nicht gefunden.");
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new InvalidOperationException("Lieferantenname ist erforderlich.");
        }

        supplier.Name = dto.Name;
        supplier.Address = dto.Address;
        supplier.PostalCode = dto.PostalCode;
        supplier.City = dto.City;
        supplier.Country = dto.Country;
        supplier.Email = dto.Email;
        supplier.Phone = dto.Phone;
        supplier.TaxId = dto.TaxId;
        supplier.Iban = dto.Iban;
        supplier.Bic = dto.Bic;
        supplier.Notes = dto.Notes;

        await _supplierRepository.UpdateAsync(supplier);
        return MapToDto(supplier);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _supplierRepository.DeleteAsync(id);
    }

    public async Task<SupplierDto?> FindByNameAsync(string name)
    {
        var supplier = await _supplierRepository.FindByNameAsync(name);
        return supplier != null ? MapToDto(supplier) : null;
    }

    public async Task<string> GenerateSupplierNumberAsync()
    {
        return await _supplierRepository.GenerateSupplierNumberAsync();
    }

    private static SupplierDto MapToDto(Supplier supplier)
    {
        return new SupplierDto
        {
            Id = supplier.Id,
            SupplierNumber = supplier.SupplierNumber,
            Name = supplier.Name,
            Address = supplier.Address,
            PostalCode = supplier.PostalCode,
            City = supplier.City,
            Country = supplier.Country,
            Email = supplier.Email,
            Phone = supplier.Phone,
            TaxId = supplier.TaxId,
            Iban = supplier.Iban,
            Bic = supplier.Bic,
            Notes = supplier.Notes,
            CreatedAt = supplier.CreatedAt,
            UpdatedAt = supplier.UpdatedAt,
            DocumentCount = supplier.Documents.Count
        };
    }
}
