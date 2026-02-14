namespace Kuestencode.Werkbank.Recepta.Controllers.Dtos;

/// <summary>
/// API-Response DTO für einen Lieferanten.
/// </summary>
public class SupplierDto
{
    public Guid Id { get; set; }
    public string SupplierNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string Country { get; set; } = "DE";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? TaxId { get; set; }
    public string? Iban { get; set; }
    public string? Bic { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int DocumentCount { get; set; }
}

/// <summary>
/// API-Request DTO zum Erstellen eines Lieferanten.
/// </summary>
public class CreateSupplierRequest
{
    public string SupplierNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string Country { get; set; } = "DE";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? TaxId { get; set; }
    public string? Iban { get; set; }
    public string? Bic { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// API-Request DTO zum Aktualisieren eines Lieferanten.
/// </summary>
public class UpdateSupplierRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string Country { get; set; } = "DE";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? TaxId { get; set; }
    public string? Iban { get; set; }
    public string? Bic { get; set; }
    public string? Notes { get; set; }
}
