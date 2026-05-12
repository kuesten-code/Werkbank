using Kuestencode.Shared.Contracts.Recepta;
using Kuestencode.Werkbank.Recepta.Data;
using Kuestencode.Werkbank.Recepta.Data.Repositories;
using Kuestencode.Werkbank.Recepta.Domain.Entities;
using Kuestencode.Werkbank.Recepta.Domain.Enums;
using Kuestencode.Werkbank.Recepta.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Recepta.Services.Implementation;

public class DocumentPaymentService : IDocumentPaymentService
{
    private readonly IDocumentPaymentRepository _paymentRepository;
    private readonly ReceptaDbContext _context;

    public DocumentPaymentService(IDocumentPaymentRepository paymentRepository, ReceptaDbContext context)
    {
        _paymentRepository = paymentRepository;
        _context = context;
    }

    public async Task<IEnumerable<DocumentPayment>> GetZahlungenAsync(Guid documentId)
    {
        return await _paymentRepository.GetByDocumentIdAsync(documentId);
    }

    public async Task ZahlungErfassenAsync(Guid documentId, decimal betrag, DateOnly datum, string? notiz)
    {
        var payment = new DocumentPayment
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            Amount = betrag,
            PaymentDate = datum,
            Notes = notiz,
            CreatedAt = DateTime.UtcNow
        };

        await _paymentRepository.AddAsync(payment);
        await RecalculateStatusAsync(documentId);
    }

    public async Task ZahlungLoeschenAsync(Guid zahlungId)
    {
        var payment = await _context.DocumentPayments.FindAsync(zahlungId);
        if (payment == null)
            throw new InvalidOperationException($"Zahlung mit ID {zahlungId} nicht gefunden.");

        var documentId = payment.DocumentId;
        await _paymentRepository.DeleteAsync(zahlungId);
        await RecalculateStatusAsync(documentId);
    }

    public async Task<IEnumerable<ReceptaPaymentDto>> GetEuerPaymentsAsync(DateOnly von, DateOnly bis)
    {
        var payments = await _context.DocumentPayments
            .Where(p => p.PaymentDate >= von && p.PaymentDate <= bis)
            .Include(p => p.Document)
                .ThenInclude(d => d.Supplier)
            .OrderBy(p => p.PaymentDate)
            .ToListAsync();

        return payments.Select(p => new ReceptaPaymentDto
        {
            PaymentId = p.Id,
            DocumentId = p.DocumentId,
            DocumentNumber = p.Document.DocumentNumber,
            SupplierName = p.Document.Supplier?.Name ?? string.Empty,
            InvoiceDate = p.Document.InvoiceDate,
            PaymentDate = p.PaymentDate,
            PaymentAmount = p.Amount,
            AmountNet = p.Document.AmountNet,
            AmountTax = p.Document.AmountTax,
            AmountGross = p.Document.AmountGross,
            TaxRate = p.Document.TaxRate,
            Category = p.Document.Category.ToString()
        });
    }

    private async Task RecalculateStatusAsync(Guid documentId)
    {
        var totalPaid = await _context.DocumentPayments
            .Where(p => p.DocumentId == documentId)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        var document = await _context.Documents.FirstOrDefaultAsync(d => d.Id == documentId);
        if (document == null) return;
        if (document.Status is DocumentStatus.Draft) return;

        var newStatus = DocumentStatusCalculator.Calculate(document.AmountGross, totalPaid);
        document.Status = newStatus;

        if (newStatus == DocumentStatus.Paid)
        {
            if (!document.PaidDate.HasValue)
            {
                var lastPayment = await _context.DocumentPayments
                    .Where(p => p.DocumentId == documentId)
                    .MaxAsync(p => (DateOnly?)p.PaymentDate);
                document.PaidDate = lastPayment;
            }
        }
        else
        {
            document.PaidDate = null;
        }

        await _context.SaveChangesAsync();
    }
}
