using Kuestencode.Werkbank.Recepta.Domain.Entities;

namespace Kuestencode.Werkbank.Recepta.Data.Repositories;

public interface IDocumentActivityRepository
{
    Task AddAsync(DocumentActivityLog entry);
    Task<IEnumerable<DocumentActivityLog>> GetRecentAsync(int count);
}
