namespace Kuestencode.Faktura.Models;

/// <summary>
/// Prüft Rabatt und Abschläge eines Belegs. Vergleicht Beträge ohne Vorzeichen, damit dieselben Regeln
/// für Rechnungen und Gutschriften gelten.
/// </summary>
public static class InvoiceAdjustmentValidator
{
    public static string? Validate(Invoice invoice) => ValidateDiscount(invoice) ?? ValidateDownPayments(invoice);

    private static string? ValidateDiscount(Invoice invoice)
    {
        if (invoice.DiscountType == DiscountType.None)
            return null;

        if (!invoice.DiscountValue.HasValue || invoice.DiscountValue.Value <= 0)
            return "Rabatt muss größer als 0 sein.";

        if (invoice.DiscountType == DiscountType.Percentage && invoice.DiscountValue.Value > 100)
            return "Prozentualer Rabatt darf nicht über 100% liegen.";

        if (invoice.DiscountType == DiscountType.Absolute && invoice.DiscountValue.Value > Math.Abs(invoice.TotalNet))
            return "Absoluter Rabatt darf die Nettosumme nicht übersteigen.";

        return null;
    }

    private static string? ValidateDownPayments(Invoice invoice)
    {
        if (invoice.DownPayments.Any(d => d.Amount <= 0))
            return "Abschlag-Betrag muss größer als 0 sein.";

        if (invoice.DownPayments.Any(d => string.IsNullOrWhiteSpace(d.Description)))
            return "Beschreibung für Abschläge ist erforderlich.";

        if (Math.Abs(invoice.TotalDownPayments) > Math.Abs(invoice.TotalGross))
            return "Abschläge dürfen die Bruttosumme nicht übersteigen.";

        return null;
    }
}
