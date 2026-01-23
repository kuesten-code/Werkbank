using Kuestencode.Core.Models;
using Kuestencode.Faktura.Models;
using Kuestencode.Faktura.Services.Pdf.Components;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Kuestencode.Faktura.Services.Pdf.Components;

/// <summary>
/// Builds the payment information section with bank details and QR code.
/// </summary>
public class PdfPaymentInfoBuilder
{
    private readonly PdfQRCodeGenerator _qrCodeGenerator;
    private readonly PdfTemplateEngine _templateEngine;
    private const string TextSecondaryColor = "#6B7280";

    public PdfPaymentInfoBuilder(PdfQRCodeGenerator qrCodeGenerator, PdfTemplateEngine templateEngine)
    {
        _qrCodeGenerator = qrCodeGenerator;
        _templateEngine = templateEngine;
    }

    /// <summary>
    /// Renders the payment information section including bank details and QR code.
    /// </summary>
    public void Render(IContainer container, Invoice invoice, Company company, bool bold = false)
    {
        container.PaddingTop(20).Row(paymentRow =>
        {
            // Left side: Payment information
            paymentRow.RelativeItem().Column(paymentColumn =>
            {
                RenderPaymentNotice(paymentColumn, invoice, company, bold);
                RenderBankDetails(paymentColumn, invoice, company);
            });

            // Right side: QR Code
            RenderQRCode(paymentRow, invoice, company);
        });
    }

    private void RenderPaymentNotice(ColumnDescriptor paymentColumn, Invoice invoice, Company company, bool bold)
    {
        string noticeText;

        if (!string.IsNullOrWhiteSpace(company.PdfPaymentNotice))
        {
            noticeText = _templateEngine.ReplacePlaceholders(company.PdfPaymentNotice, invoice, company);
        }
        else
        {
            noticeText = invoice.DueDate.HasValue
                ? $"Bitte überweisen Sie den Betrag bis zum {invoice.DueDate.Value:dd.MM.yyyy} auf folgendes Konto:"
                : "Bitte überweisen Sie den Betrag auf folgendes Konto:";
        }

        var textStyle = paymentColumn.Item().Text(noticeText).FontSize(10);
        if (bold) textStyle.Bold();
    }

    private void RenderBankDetails(ColumnDescriptor paymentColumn, Invoice invoice, Company company)
    {
        paymentColumn.Item().PaddingTop(8).Column(bankColumn =>
        {
            bankColumn.Item().Text($"Bankname: {company.BankName}").FontSize(9);
            bankColumn.Item().Text($"IBAN: {company.BankAccount}").FontSize(9);

            if (!string.IsNullOrEmpty(company.Bic))
            {
                bankColumn.Item().Text($"BIC: {company.Bic}").FontSize(9);
            }

            var accountHolder = !string.IsNullOrWhiteSpace(company.AccountHolder)
                ? company.AccountHolder
                : company.OwnerFullName;

            bankColumn.Item().Text($"Kontoinhaber: {accountHolder}").FontSize(9);
            bankColumn.Item().PaddingTop(3).Text($"Verwendungszweck: {invoice.InvoiceNumber}")
                .FontSize(9).Bold();
        });
    }

    private void RenderQRCode(RowDescriptor paymentRow, Invoice invoice, Company company)
    {
        paymentRow.ConstantItem(100).AlignRight().Column(qrColumn =>
        {
            var qrCodeBytes = _qrCodeGenerator.GenerateGiroCodeQR(invoice, company);
            qrColumn.Item().Width(80).Height(80).Image(qrCodeBytes);
            qrColumn.Item().PaddingTop(3).Text("QR-Code für Überweisung")
                .FontSize(7)
                .FontColor(TextSecondaryColor)
                .AlignCenter();
        });
    }
}
