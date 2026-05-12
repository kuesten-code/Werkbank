using Kuestencode.Werkbank.Recepta.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Recepta.Data.Repositories;

public class DocumentPaymentRepository : IDocumentPaymentRepository
{
    private readonly ReceptaDbContext _context;

    public DocumentPaymentRepository(ReceptaDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DocumentPayment>> GetByDocumentIdAsync(Guid documentId)
    {
        return await _context.DocumentPayments
            .Where(p => p.DocumentId == documentId)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();
    }

    public async Task AddAsync(DocumentPayment payment)
    {
        await _context.DocumentPayments.AddAsync(payment);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var payment = await _context.DocumentPayments.FindAsync(id);
        if (payment == null)
            throw new InvalidOperationException($"Zahlung mit ID {id} nicht gefunden.");

        _context.DocumentPayments.Remove(payment);
        await _context.SaveChangesAsync();
    }
}
