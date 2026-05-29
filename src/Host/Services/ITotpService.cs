using Kuestencode.Werkbank.Host.Models;

namespace Kuestencode.Werkbank.Host.Services;

public interface ITotpService
{
    string GenerateSecret();
    string GetQrCodeSvg(string secret, string email, string issuer);
    bool VerifyCode(string secret, string code);
    string[] GenerateRecoveryCodes();
    string HashRecoveryCode(string code);
    bool VerifyAndConsumeRecoveryCode(TeamMember member, string code);
}
