using QRCoder;

namespace Kuestencode.Shared.Pdf.Components;

/// <summary>
/// Generiert QR-Codes für PDF-Dokumente.
/// Unterstützt GiroCode (EPC-Standard) für SEPA-Überweisungen.
/// </summary>
public class PdfQRCodeGenerator
{
    /// <summary>
    /// Generiert einen GiroCode QR-Code für SEPA-Überweisungen.
    /// </summary>
    /// <param name="bic">BIC der Bank (optional)</param>
    /// <param name="accountHolder">Name des Kontoinhabers</param>
    /// <param name="iban">IBAN</param>
    /// <param name="amount">Betrag</param>
    /// <param name="reference">Verwendungszweck/Referenz</param>
    /// <returns>QR-Code als PNG-Bytes oder null bei Fehler</returns>
    public byte[]? GenerateGiroCodeQR(
        string? bic,
        string accountHolder,
        string iban,
        decimal amount,
        string reference)
    {
        try
        {
            // GiroCode Format (EPC-Standard)
            // BCD - Service Tag
            // 002 - Version
            // 1 - Character Set (UTF-8)
            // SCT - Identification (SEPA Credit Transfer)
            var giroCodeData = string.Join("\n",
                "BCD",                                    // Service Tag
                "002",                                    // Version
                "1",                                      // Character Set: UTF-8
                "SCT",                                    // SEPA Credit Transfer
                bic ?? "",                                // BIC
                accountHolder,                            // Beneficiary Name
                iban.Replace(" ", ""),                    // IBAN (ohne Leerzeichen)
                $"EUR{amount:F2}",                        // Amount
                "",                                       // Purpose (empty)
                reference,                                // Remittance Structured
                "",                                       // Remittance Unstructured
                ""                                        // Beneficiary to Originator Info
            );

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(giroCodeData, QRCodeGenerator.ECCLevel.M);
            using var qrCode = new PngByteQRCode(qrCodeData);

            return qrCode.GetGraphic(20); // 20 pixels per module
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Generiert einen einfachen QR-Code mit beliebigem Text.
    /// </summary>
    /// <param name="content">Inhalt des QR-Codes</param>
    /// <param name="pixelsPerModule">Pixel pro Modul (Standard: 20)</param>
    /// <returns>QR-Code als PNG-Bytes oder null bei Fehler</returns>
    public byte[]? GenerateQRCode(string content, int pixelsPerModule = 20)
    {
        try
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
            using var qrCode = new PngByteQRCode(qrCodeData);

            return qrCode.GetGraphic(pixelsPerModule);
        }
        catch
        {
            return null;
        }
    }
}
