using System.Globalization;

namespace Kuestencode.Faktura.Models;

/// <summary>
/// Formuliert das Skonto-Angebot einer Rechnung einheitlich für PDF, E-Mail und Oberfläche.
/// </summary>
public static class SkontoTerms
{
    private static readonly CultureInfo Culture = new("de-DE");

    public static string? Describe(Invoice invoice)
    {
        if (invoice.Type == InvoiceType.CreditNote || invoice.SkontoAmount == 0 || !invoice.SkontoDeadline.HasValue)
            return null;

        var skontoAmount = invoice.SkontoAmount.ToString("C2", Culture);
        var offer = invoice.SkontoType == DiscountType.Percentage
            ? $"{invoice.SkontoValue!.Value.ToString("0.##", Culture)} % Skonto ({skontoAmount})"
            : $"{skontoAmount} Skonto";

        return $"Bei Zahlung bis zum {invoice.SkontoDeadline.Value:dd.MM.yyyy} gewähren wir {offer}. " +
               $"Zahlbetrag dann: {invoice.AmountDueWithSkonto.ToString("C2", Culture)}";
    }
}
