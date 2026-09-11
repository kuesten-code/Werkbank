using Kuestencode.Werkbank.Acta.Domain.Entities;

namespace Kuestencode.Werkbank.Acta.Data.Repositories;

public interface IPinnedProjectRepository
{
    Task<List<PinnedProject>> GetByUserIdAsync(Guid userId);
    Task<bool> ExistsAsync(Guid userId, Guid projectId);
    Task AddAsync(PinnedProject pin);
    Task<bool> RemoveAsync(Guid userId, Guid projectId);
}
