using Kuestencode.Werkbank.Host.Models;

namespace Kuestencode.Werkbank.Host.Services;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string email, string password);
    Task<TeamMember?> GetCurrentUserAsync(string? userId);
    string GenerateToken(TeamMember member);
}
