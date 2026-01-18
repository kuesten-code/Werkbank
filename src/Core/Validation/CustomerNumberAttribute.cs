using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Kuestencode.Core.Validation;

/// <summary>
/// Validates customer number format (K + 5 digits, e.g., K00001).
/// </summary>
public partial class CustomerNumberAttribute : ValidationAttribute
{
    [GeneratedRegex(@"^K\d{5}$", RegexOptions.Compiled)]
    private static partial Regex CustomerNumberRegex();

    public CustomerNumberAttribute()
    {
        ErrorMessage = "Die Kundennummer muss im Format 'K00001' sein.";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return new ValidationResult("Kundennummer ist erforderlich.");
        }

        var customerNumber = value.ToString()!;

        if (!CustomerNumberRegex().IsMatch(customerNumber))
        {
            return new ValidationResult(ErrorMessage);
        }

        return ValidationResult.Success;
    }

    /// <summary>
    /// Generates the next customer number based on the current highest number.
    /// </summary>
    public static string GenerateNext(int currentHighest)
    {
        return $"K{(currentHighest + 1):D5}";
    }
}
