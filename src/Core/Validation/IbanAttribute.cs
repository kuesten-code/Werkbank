using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Kuestencode.Core.Validation;

/// <summary>
/// Validates IBAN format (currently supports German IBAN: DE + 20 digits).
/// </summary>
public partial class IbanAttribute : ValidationAttribute
{
    [GeneratedRegex(@"^DE\d{20}$", RegexOptions.Compiled)]
    private static partial Regex IbanRegex();

    public IbanAttribute()
    {
        ErrorMessage = "Ungültige IBAN (Format: DE + 20 Ziffern)";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
        {
            return ValidationResult.Success;
        }

        var iban = value.ToString()!.Replace(" ", "").ToUpper();

        if (!IbanRegex().IsMatch(iban))
        {
            return new ValidationResult(ErrorMessage);
        }

        return ValidationResult.Success;
    }

    /// <summary>
    /// Formats an IBAN with spaces for better readability.
    /// </summary>
    public static string Format(string iban)
    {
        if (string.IsNullOrWhiteSpace(iban))
            return iban;

        var cleaned = iban.Replace(" ", "").ToUpper();
        var formatted = string.Join(" ",
            Enumerable.Range(0, (int)Math.Ceiling(cleaned.Length / 4.0))
                .Select(i => cleaned.Substring(i * 4, Math.Min(4, cleaned.Length - i * 4))));

        return formatted;
    }
}
