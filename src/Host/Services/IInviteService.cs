using Kuestencode.Werkbank.Host.Models;

namespace Kuestencode.Werkbank.Host.Services;

public interface IInviteService
{
    Task<string> CreateInviteAsync(Guid teamMemberId);
    Task<TeamMember?> ValidateInviteTokenAsync(string token);
    Task AcceptInviteAsync(string token, string password);
    Task ResendInviteAsync(Guid teamMemberId);
}
