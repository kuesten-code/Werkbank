using System.ComponentModel.DataAnnotations;

namespace Kuestencode.Core.Models;

/// <summary>
/// Represents a customer entity.
/// </summary>
public class Customer : BaseEntity
{
    [Required(ErrorMessage = "Kundennummer ist erforderlich")]
    [MaxLength(20)]
    public string CustomerNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name ist erforderlich")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Adresse ist erforderlich")]
    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "PLZ ist erforderlich")]
    [MaxLength(10)]
    public string PostalCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Stadt ist erforderlich")]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Country { get; set; } = "Deutschland";

    [EmailAddress(ErrorMessage = "Ungültige Email-Adresse")]
    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Returns the full formatted address.
    /// </summary>
    public string GetFormattedAddress()
    {
        return $"{Address}\n{PostalCode} {City}\n{Country}";
    }
}
