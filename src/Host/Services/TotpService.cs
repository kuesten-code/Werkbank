using System.Security.Cryptography;
using System.Text.Json;
using Kuestencode.Werkbank.Host.Models;
using OtpNet;
using QRCoder;

namespace Kuestencode.Werkbank.Host.Services;

public class TotpService : ITotpService
{
    private readonly IPasswordService _passwordService;

    public TotpService(IPasswordService passwordService)
    {
        _passwordService = passwordService;
    }

    public string GenerateSecret()
    {
        var key = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(key);
    }

    public string GetQrCodeSvg(string secret, string email, string issuer)
    {
        var uri = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}" +
                  $"?secret={secret}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits=6&period=30";

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
        using var code = new SvgQRCode(data);
        return code.GetGraphic(5);
    }

    public bool VerifyCode(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length != 6)
            return false;

        var key = Base32Encoding.ToBytes(secret);
        var totp = new Totp(key);
        return totp.VerifyTotp(code.Trim(), out _, new VerificationWindow(1, 1));
    }

    public string[] GenerateRecoveryCodes()
    {
        return Enumerable.Range(0, 8)
            .Select(_ => $"{GenerateCodeSegment()}-{GenerateCodeSegment()}")
            .ToArray();
    }

    public string HashRecoveryCode(string code)
        => _passwordService.HashPassword(code.Trim().ToUpperInvariant());

    public bool VerifyAndConsumeRecoveryCode(TeamMember member, string code)
    {
        if (string.IsNullOrEmpty(member.MfaRecoveryCodes))
            return false;

        var hashes = JsonSerializer.Deserialize<List<string>>(member.MfaRecoveryCodes) ?? [];
        var normalized = code.Trim().ToUpperInvariant();

        var matchIndex = hashes.FindIndex(h => _passwordService.VerifyPassword(h, normalized));
        if (matchIndex < 0)
            return false;

        hashes.RemoveAt(matchIndex);
        member.MfaRecoveryCodes = JsonSerializer.Serialize(hashes);
        return true;
    }

    private static string GenerateCodeSegment()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return string.Create(6, chars, static (span, alphabet) =>
        {
            var bytes = RandomNumberGenerator.GetBytes(span.Length);
            for (var i = 0; i < span.Length; i++)
                span[i] = alphabet[bytes[i] % alphabet.Length];
        });
    }
}
