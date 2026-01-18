using System.ComponentModel.DataAnnotations;

namespace Kuestencode.Core.Models;

/// <summary>
/// Value object representing a postal address.
/// Can be used for both company and customer addresses.
/// </summary>
public class Address
{
    [Required(ErrorMessage = "Adresse ist erforderlich")]
    [MaxLength(500)]
    public string Street { get; set; } = string.Empty;

    [Required(ErrorMessage = "PLZ ist erforderlich")]
    [MaxLength(10)]
    public string PostalCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Stadt ist erforderlich")]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Land ist erforderlich")]
    [MaxLength(100)]
    public string Country { get; set; } = "Deutschland";

    /// <summary>
    /// Returns the full formatted address.
    /// </summary>
    public string GetFormattedAddress()
    {
        return $"{Street}\n{PostalCode} {City}\n{Country}";
    }

    /// <summary>
    /// Returns a single-line formatted address.
    /// </summary>
    public string GetSingleLineAddress()
    {
        return $"{Street}, {PostalCode} {City}, {Country}";
    }
}
