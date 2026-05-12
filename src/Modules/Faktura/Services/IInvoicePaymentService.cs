using Kuestencode.Faktura.Data;
using Kuestencode.Faktura.Data.Repositories;
using Kuestencode.Faktura.Models;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Faktura.Services;

public interface IInvoicePaymentService
{
    Task<IEnumerable<InvoicePayment>> GetZahlungenAsync(int invoiceId);
    Task ZahlungErfassenAsync(int invoiceId, decimal betrag, DateTime datum, string? notiz);
    Task ZahlungLoeschenAsync(int zahlungId);
    Task<IEnumerable<InvoicePayment>> GetByPaymentDateRangeAsync(DateTime von, DateTime bis);
}

public class InvoicePaymentService : IInvoicePaymentService
{
    private readonly IInvoicePaymentRepository _paymentRepository;
    private readonly FakturaDbContext _context;
    private readonly ILogger<InvoicePaymentService> _logger;

    public InvoicePaymentService(
        IInvoicePaymentRepository paymentRepository,
        FakturaDbContext context,
        ILogger<InvoicePaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<InvoicePayment>> GetZahlungenAsync(int invoiceId)
    {
        return await _paymentRepository.GetByInvoiceIdAsync(invoiceId);
    }

    public async Task ZahlungErfassenAsync(int invoiceId, decimal betrag, DateTime datum, string? notiz)
    {
        var payment = new InvoicePayment
        {
            InvoiceId = invoiceId,
            Amount = betrag,
            PaymentDate = DateTime.SpecifyKind(datum.Date, DateTimeKind.Utc),
            Notes = notiz
        };
        await _paymentRepository.AddAsync(payment);
        await RecalculateStatusAsync(invoiceId);
    }

    public async Task ZahlungLoeschenAsync(int zahlungId)
    {
        var payment = await _paymentRepository.GetByIdAsync(zahlungId)
            ?? throw new InvalidOperationException($"Zahlung {zahlungId} nicht gefunden.");

        var invoiceId = payment.InvoiceId;
        await _paymentRepository.DeleteAsync(payment);
        await RecalculateStatusAsync(invoiceId);
    }

    public async Task<IEnumerable<InvoicePayment>> GetByPaymentDateRangeAsync(DateTime von, DateTime bis)
    {
        return await _paymentRepository.GetByPaymentDateRangeAsync(von, bis);
    }

    private async Task RecalculateStatusAsync(int invoiceId)
    {
        var totalPaid = await _context.InvoicePayments
            .Where(p => p.InvoiceId == invoiceId)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        var invoice = await _context.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice == null) return;
        if (invoice.Status is InvoiceStatus.Draft or InvoiceStatus.Cancelled) return;

        var newStatus = InvoiceStatusCalculator.Calculate(invoice.TotalGross, totalPaid, invoice.DueDate);
        invoice.Status = newStatus;

        if (newStatus == InvoiceStatus.Paid)
        {
            if (!invoice.PaidDate.HasValue)
            {
                invoice.PaidDate = await _context.InvoicePayments
                    .Where(p => p.InvoiceId == invoiceId)
                    .MaxAsync(p => (DateTime?)p.PaymentDate);
            }
        }
        else
        {
            invoice.PaidDate = null;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Status für Rechnung {InvoiceId} neu berechnet: {Status}", invoiceId, newStatus);
    }
}
