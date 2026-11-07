using Kuestencode.Faktura.Data.Repositories;
using Kuestencode.Faktura.Models;

namespace Kuestencode.Faktura.Services;

public interface ILegacyInvoiceImportService
{
    Task<Invoice> ImportAsync(LegacyInvoiceImport import);
}

/// <summary>
/// Übernimmt offene Rechnungen aus einem Vorsystem: freie Rechnungsnummer (kein Nummernkreis),
/// Summen statt Positionen, Original-PDF als unveränderlicher Beleg.
/// </summary>
public class LegacyInvoiceImportService : ILegacyInvoiceImportService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IInvoicePaymentService _paymentService;
    private readonly ILogger<LegacyInvoiceImportService> _logger;

    public LegacyInvoiceImportService(
        IInvoiceRepository invoiceRepository,
        IInvoicePaymentService paymentService,
        ILogger<LegacyInvoiceImportService> logger)
    {
        _invoiceRepository = invoiceRepository;
        _paymentService = paymentService;
        _logger = logger;
    }

    public async Task<Invoice> ImportAsync(LegacyInvoiceImport import)
    {
        Validate(import);

        var invoiceNumber = import.InvoiceNumber.Trim();
        if (await _invoiceRepository.InvoiceNumberExistsAsync(invoiceNumber))
            throw new InvalidOperationException($"Rechnungsnummer {invoiceNumber} existiert bereits.");

        var invoice = new Invoice
        {
            InvoiceNumber = invoiceNumber,
            CustomerId = import.CustomerId,
            InvoiceDate = ToUtcDate(import.InvoiceDate),
            DueDate = import.DueDate.HasValue ? ToUtcDate(import.DueDate.Value) : null,
            Notes = string.IsNullOrWhiteSpace(import.Notes) ? null : import.Notes.Trim(),
            Type = InvoiceType.Invoice,
            IsImported = true,
            Items = [import.ToSummaryItem()],
            Attachments =
            [
                new InvoiceAttachment
                {
                    FileName = import.OriginalPdfFileName,
                    ContentType = "application/pdf",
                    FileSize = import.OriginalPdf.Length,
                    Data = import.OriginalPdf,
                    IsFrozenSnapshot = true
                }
            ]
        };
        invoice.Status = InvoiceStatusCalculator.Calculate(invoice.SettlementAmount, 0, invoice.DueDate);

        await _invoiceRepository.AddAsync(invoice);

        // Erst nach dem Speichern erfassen: das Audit-Log ordnet die Zahlung über die bereits vergebene Rechnungs-Id zu
        if (import.AlreadyPaid > 0)
        {
            await _paymentService.ZahlungErfassenAsync(
                invoice.Id, import.AlreadyPaid, import.AlreadyPaidDate!.Value, "Teilzahlung laut Vorsystem");
        }

        _logger.LogInformation("Altrechnung {InvoiceNumber} aus Vorsystem übernommen", invoiceNumber);
        return invoice;
    }

    private static void Validate(LegacyInvoiceImport import)
    {
        var invoiceNumber = import.InvoiceNumber.Trim();

        if (invoiceNumber.Length == 0)
            throw new ArgumentException("Rechnungsnummer ist erforderlich.");
        if (invoiceNumber.Length > LegacyInvoiceImport.MaxInvoiceNumberLength)
            throw new ArgumentException($"Rechnungsnummer darf höchstens {LegacyInvoiceImport.MaxInvoiceNumberLength} Zeichen lang sein.");
        if (import.CustomerId <= 0)
            throw new ArgumentException("Kunde ist erforderlich.");
        if (import.NetAmount <= 0)
            throw new ArgumentException("Nettobetrag muss größer als 0 sein.");
        if (!LegacyInvoiceImport.AllowedVatRates.Contains(import.VatRate))
            throw new ArgumentException($"MwSt-Satz {import.VatRate} % wird nicht unterstützt.");
        if (import.AlreadyPaid < 0)
            throw new ArgumentException("Bereits gezahlter Betrag darf nicht negativ sein.");
        if (import.AlreadyPaid >= import.GrossAmount)
            throw new ArgumentException("Vollständig bezahlte Rechnungen sind kein offener Posten und werden nicht übernommen.");
        if (import.AlreadyPaid > 0 && !import.AlreadyPaidDate.HasValue)
            throw new ArgumentException("Für die Teilzahlung ist ein Zahlungsdatum erforderlich.");
        if (import.OriginalPdf.Length == 0)
            throw new ArgumentException("Das Original-PDF ist erforderlich.");
    }

    private static DateTime ToUtcDate(DateTime date) => DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
}
