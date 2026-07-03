using Kuestencode.Core.Models;
using Kuestencode.Faktura.Models;
using System.Globalization;

namespace Kuestencode.Faktura.Services.Email;

/// <summary>
/// Rendert den Rechnungs-Inhalt für Emails (Details-Tabelle, Bankverbindung).
/// Layout/Farben/Anrede/Grußformel kommen zentral vom Host-EmailEngine.
/// </summary>
public class HtmlEmailTemplateRenderer : IEmailTemplateRenderer
{
    public string RenderContentHtml(Invoice invoice, Company company)
    {
        var culture = new CultureInfo("de-DE");
        var paymentAmount = invoice.TotalDownPayments > 0 ? invoice.AmountDue : invoice.TotalGross;
        var formattedTotal = paymentAmount.ToString("C", culture);
        var formattedDate = invoice.InvoiceDate.ToString("dd.MM.yyyy", culture);
        var formattedDueDate = invoice.DueDate?.ToString("dd.MM.yyyy", culture) ?? "Sofort fällig";

        return $"""
            <p>anbei erhalten Sie die Rechnung <strong>{invoice.InvoiceNumber}</strong>.</p>
            <table style="width:100%; border-collapse:collapse; background-color:white; padding:15px; margin:15px 0;">
                <tr><td><strong>Rechnungsbetrag:</strong></td><td><strong>{formattedTotal}</strong></td></tr>
                {(invoice.DiscountAmount > 0 ? $"""<tr><td colspan="2" style="font-size:12px; color:#666;"><em>inkl. Rabatt: {(invoice.DiscountType == DiscountType.Percentage ? $"{invoice.DiscountValue}%" : invoice.DiscountAmount.ToString("C", culture))}</em></td></tr>""" : "")}
                <tr><td><strong>Rechnungsnummer:</strong></td><td>{invoice.InvoiceNumber}</td></tr>
                <tr><td><strong>Rechnungsdatum:</strong></td><td>{formattedDate}</td></tr>
                <tr><td><strong>Fällig am:</strong></td><td>{formattedDueDate}</td></tr>
            </table>
            <table style="width:100%; border-collapse:collapse; background-color:white; border:1px solid #ddd; padding:15px; margin:15px 0;">
                <tr><td colspan="2"><strong>Bankverbindung</strong></td></tr>
                <tr><td><strong>Kontoinhaber:</strong></td><td>{company.AccountHolder ?? company.OwnerFullName}</td></tr>
                <tr><td><strong>Bank:</strong></td><td>{company.BankName}</td></tr>
                <tr><td><strong>IBAN:</strong></td><td>{company.BankAccount}</td></tr>
                {(!string.IsNullOrWhiteSpace(company.Bic) ? $"""<tr><td><strong>BIC:</strong></td><td>{company.Bic}</td></tr>""" : "")}
                <tr><td><strong>Verwendungszweck:</strong></td><td>{invoice.InvoiceNumber}</td></tr>
            </table>
            <p>Die Rechnung finden Sie im Anhang dieser E-Mail als PDF-Datei.</p>
            """;
    }

    public string RenderContentText(Invoice invoice, Company company)
    {
        var culture = new CultureInfo("de-DE");
        var paymentAmount = invoice.TotalDownPayments > 0 ? invoice.AmountDue : invoice.TotalGross;
        var formattedTotal = paymentAmount.ToString("C", culture);
        var formattedDate = invoice.InvoiceDate.ToString("dd.MM.yyyy", culture);
        var formattedDueDate = invoice.DueDate?.ToString("dd.MM.yyyy", culture) ?? "Sofort fällig";

        return $"""
            anbei erhalten Sie die Rechnung {invoice.InvoiceNumber}.

            RECHNUNGSDETAILS:
            ------------------
            Rechnungsbetrag: {formattedTotal}
            {(invoice.DiscountAmount > 0 ? $"  (inkl. Rabatt: {(invoice.DiscountType == DiscountType.Percentage ? $"{invoice.DiscountValue}%" : invoice.DiscountAmount.ToString("C", culture))})\n" : "")}Rechnungsnummer: {invoice.InvoiceNumber}
            Rechnungsdatum:  {formattedDate}
            Fällig am:       {formattedDueDate}

            BANKVERBINDUNG:
            ---------------
            Kontoinhaber:    {company.AccountHolder ?? company.OwnerFullName}
            Bank:            {company.BankName}
            IBAN:            {company.BankAccount}
            {(!string.IsNullOrWhiteSpace(company.Bic) ? $"BIC:             {company.Bic}\n" : "")}Verwendungszweck: {invoice.InvoiceNumber}

            Die Rechnung finden Sie im Anhang dieser E-Mail als PDF-Datei.
            """;
    }

    public string? ResolveGreeting(Invoice invoice, string? customMessage)
    {
        if (!string.IsNullOrWhiteSpace(customMessage))
        {
            return customMessage;
        }

        if (!string.IsNullOrWhiteSpace(invoice.Customer?.Salutation))
        {
            return invoice.Customer.Salutation;
        }

        return null;
    }
}
