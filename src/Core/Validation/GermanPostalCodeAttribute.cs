using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Kuestencode.Core.Validation;

/// <summary>
/// Validates German postal codes (5 digits).
/// </summary>
public partial class GermanPostalCodeAttribute : ValidationAttribute
{
    [GeneratedRegex(@"^\d{5}$", RegexOptions.Compiled)]
    private static partial Regex PostalCodeRegex();

    public GermanPostalCodeAttribute()
    {
        ErrorMessage = "Die Postleitzahl muss aus 5 Ziffern bestehen.";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return ValidationResult.Success;
        }

        var postalCode = value.ToString()!;

        if (!PostalCodeRegex().IsMatch(postalCode))
        {
            return new ValidationResult(ErrorMessage);
        }

        return ValidationResult.Success;
    }
}
