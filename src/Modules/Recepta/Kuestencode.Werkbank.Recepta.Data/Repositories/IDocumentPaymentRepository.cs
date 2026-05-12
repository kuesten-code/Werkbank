using Kuestencode.Werkbank.Recepta.Domain.Entities;

namespace Kuestencode.Werkbank.Recepta.Data.Repositories;

public interface IDocumentPaymentRepository
{
    Task<IEnumerable<DocumentPayment>> GetByDocumentIdAsync(Guid documentId);
    Task AddAsync(DocumentPayment payment);
    Task DeleteAsync(Guid id);
}
