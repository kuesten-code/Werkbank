using Kuestencode.Core.Models;
using Kuestencode.Faktura.Models;
using QRCoder;

namespace Kuestencode.Faktura.Services.Pdf.Components;

/// <summary>
/// Generates QR codes for payment information (GiroCode format).
/// </summary>
public class PdfQRCodeGenerator
{
    /// <summary>
    /// Generates a GiroCode QR code for bank transfer information.
    /// </summary>
    /// <param name="invoice">The invoice containing payment details</param>
    /// <param name="company">The company containing bank account information</param>
    /// <returns>Byte array containing PNG image of QR code</returns>
    public byte[] GenerateGiroCodeQR(Invoice invoice, Company company)
    {
        // GiroCode (EPC QR-Code) Format - European Payment Council Standard
        var accountHolder = !string.IsNullOrWhiteSpace(company.AccountHolder)
            ? company.AccountHolder
            : company.OwnerFullName;

        // Use AmountDue if there are down payments, otherwise use TotalGross
        var paymentAmount = invoice.TotalDownPayments > 0 ? invoice.AmountDue : invoice.TotalGross;
        var amount = paymentAmount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        var purpose = $"Rechnung {invoice.InvoiceNumber}";

        // GiroCode Format
        var giroCodeData = string.Join("\n", new[]
        {
            "BCD",                          // Service Tag
            "002",                          // Version
            "1",                            // Character Set (1 = UTF-8)
            "SCT",                          // Identification
            company.Bic ?? "",              // BIC (optional)
            accountHolder,                  // Beneficiary Name
            company.BankAccount,            // Beneficiary Account (IBAN)
            $"EUR{amount}",                 // Amount
            "",                             // Purpose (empty)
            purpose,                        // Remittance Information (Structured)
            "",                             // Remittance Information (Unstructured)
            ""                              // Beneficiary to originator information
        });

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(giroCodeData, QRCodeGenerator.ECCLevel.M);

        // Use SkiaSharp for cross-platform QR code rendering
        var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeBytes = qrCode.GetGraphic(20);

        return qrCodeBytes;
    }
}
