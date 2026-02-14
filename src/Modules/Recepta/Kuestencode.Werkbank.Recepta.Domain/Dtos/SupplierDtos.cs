namespace Kuestencode.Werkbank.Recepta.Domain.Dtos;

/// <summary>
/// DTO zum Erstellen eines neuen Lieferanten.
/// </summary>
public class CreateSupplierDto
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
/// DTO zum Aktualisieren eines Lieferanten.
/// </summary>
public class UpdateSupplierDto
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
