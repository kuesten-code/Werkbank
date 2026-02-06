using Kuestencode.Werkbank.Host.Models;

namespace Kuestencode.Werkbank.Host.Services;

public interface ITeamMemberService
{
    Task<List<TeamMember>> GetAllAsync(bool includeInactive = false);
    Task<TeamMember?> GetByIdAsync(Guid id);
    Task<TeamMember> CreateAsync(TeamMember member);
    Task UpdateAsync(TeamMember member);
    Task DeleteAsync(Guid id);
}

