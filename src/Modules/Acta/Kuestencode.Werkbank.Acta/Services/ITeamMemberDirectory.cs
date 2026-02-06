using Kuestencode.Werkbank.Acta.Models;

namespace Kuestencode.Werkbank.Acta.Services;

/// <summary>
/// Provides access to assignable team members.
/// </summary>
public interface ITeamMemberDirectory
{
    Task<IReadOnlyList<TeamMember>> GetAllAsync();
    Task<TeamMember?> GetByIdAsync(Guid id);
}
