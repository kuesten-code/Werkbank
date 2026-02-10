using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.Contracts.Host;
using Kuestencode.Werkbank.Acta.Models;

namespace Kuestencode.Werkbank.Acta.Services;

/// <summary>
/// Loads assignable team members from the Host service so other modules see the same directory.
/// </summary>
public class HostTeamMemberDirectory : ITeamMemberDirectory
{
    private readonly IHostApiClient _hostApiClient;
    private IReadOnlyList<TeamMember>? _cache;
    private readonly SemaphoreSlim _sync = new(1, 1);

    public HostTeamMemberDirectory(IHostApiClient hostApiClient)
    {
        _hostApiClient = hostApiClient;
    }

    public async Task<IReadOnlyList<TeamMember>> GetAllAsync()
    {
        if (_cache != null)
        {
            return _cache;
        }

        await _sync.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_cache != null)
                return _cache;

            var dtos = await _hostApiClient.GetTeamMembersAsync().ConfigureAwait(false);
            _cache = dtos
                .Select(Map)
                .Where(member => member != null)
                .Select(member => member!)
                .OrderBy(member => member.DisplayName)
                .ToList();
        }
        finally
        {
            _sync.Release();
        }

        return _cache;
    }

    public async Task<TeamMember?> GetByIdAsync(Guid id)
    {
        var members = await GetAllAsync().ConfigureAwait(false);
        return members.FirstOrDefault(member => member.Id == id);
    }

    private static TeamMember? Map(TeamMemberDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DisplayName))
        {
            return null;
        }

        return new TeamMember
        {
            Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id,
            DisplayName = dto.DisplayName.Trim(),
            Email = dto.Email
        };
    }
}
