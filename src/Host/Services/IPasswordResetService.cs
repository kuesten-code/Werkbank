using Kuestencode.Werkbank.Host.Models;

namespace Kuestencode.Werkbank.Host.Services;

public interface IPasswordResetService
{
    Task<bool> RequestResetAsync(string email);
    Task<TeamMember?> ValidateResetTokenAsync(string token);
    Task ResetPasswordAsync(string token, string newPassword);
}
