using System.ComponentModel.DataAnnotations;
using Kuestencode.Faktura.Validation;

namespace Kuestencode.Faktura.Models.CompanyConfiguration;

/// <summary>
/// Value Object representing bank account information.
/// Owned by Company entity - no separate table.
/// </summary>
public class BankAccount
{
    [Required(ErrorMessage = "Bankname ist erforderlich")]
    [MaxLength(100)]
    public string BankName { get; set; } = string.Empty;

    [Required(ErrorMessage = "IBAN ist erforderlich")]
    [Iban]
    [MaxLength(50)]
    public string Iban { get; set; } = string.Empty;

    [MaxLength(11)]
    public string? Bic { get; set; }

    [MaxLength(200)]
    public string? AccountHolder { get; set; }
}
