namespace Kuestencode.Core.Enums;

/// <summary>
/// Payment methods for invoices and transactions.
/// </summary>
public enum PaymentMethod
{
    /// <summary>
    /// Bank transfer (SEPA)
    /// </summary>
    BankTransfer = 1,

    /// <summary>
    /// Cash payment
    /// </summary>
    Cash = 2,

    /// <summary>
    /// PayPal
    /// </summary>
    PayPal = 3,

    /// <summary>
    /// Credit card
    /// </summary>
    CreditCard = 4,

    /// <summary>
    /// Direct debit (SEPA Lastschrift)
    /// </summary>
    DirectDebit = 5
}

/// <summary>
/// Extension methods for PaymentMethod enum.
/// </summary>
public static class PaymentMethodExtensions
{
    /// <summary>
    /// Returns the display name for the payment method.
    /// </summary>
    public static string ToDisplayName(this PaymentMethod method)
    {
        return method switch
        {
            PaymentMethod.BankTransfer => "Überweisung",
            PaymentMethod.Cash => "Barzahlung",
            PaymentMethod.PayPal => "PayPal",
            PaymentMethod.CreditCard => "Kreditkarte",
            PaymentMethod.DirectDebit => "SEPA-Lastschrift",
            _ => "Überweisung"
        };
    }

    /// <summary>
    /// Returns the UNCL4461 payment means code for XRechnung/ZUGFeRD.
    /// </summary>
    public static string ToPaymentMeansCode(this PaymentMethod method)
    {
        return method switch
        {
            PaymentMethod.BankTransfer => "58", // SEPA Credit Transfer
            PaymentMethod.Cash => "10",         // In Cash
            PaymentMethod.PayPal => "ZZZ",      // Other
            PaymentMethod.CreditCard => "48",   // Credit Card
            PaymentMethod.DirectDebit => "59",  // SEPA Direct Debit
            _ => "58"
        };
    }
}
