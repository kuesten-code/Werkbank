using System.ComponentModel.DataAnnotations;

namespace Kuestencode.Core.Validation;

/// <summary>
/// Validates that a name contains at least first and last name (space separated).
/// </summary>
public class FullNameAttribute : ValidationAttribute
{
    public FullNameAttribute()
    {
        ErrorMessage = "Bitte geben Sie Vor- und Nachnamen ein";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string fullName)
        {
            return ValidationResult.Success;
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return ValidationResult.Success;
        }

        // Check if the name contains at least one space (indicates first and last name)
        if (!fullName.Trim().Contains(' '))
        {
            return new ValidationResult(ErrorMessage);
        }

        return ValidationResult.Success;
    }
}
