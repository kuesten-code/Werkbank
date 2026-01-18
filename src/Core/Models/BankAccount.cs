using System.ComponentModel.DataAnnotations;
using Kuestencode.Core.Validation;

namespace Kuestencode.Core.Models;

/// <summary>
/// Value object representing bank account information.
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
