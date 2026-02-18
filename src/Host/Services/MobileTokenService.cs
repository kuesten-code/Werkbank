using System.Security.Cryptography;
using System.Text;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Models;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Host.Services;

public class MobileTokenService : IMobileTokenService
{
    private readonly HostDbContext _db;
    private readonly IPasswordService _passwordService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<MobileTokenService> _logger;

    private static readonly string[] ForbiddenPins = new[]
    {
        "0000", "1111", "2222", "3333", "4444",
        "5555", "6666", "7777", "8888", "9999",
        "1234", "4321", "0123", "3210", "2580"
    };

    public MobileTokenService(
        HostDbContext db,
        IPasswordService passwordService,
        IJwtTokenService jwtTokenService,
        ILogger<MobileTokenService> logger)
    {
        _db = db;
        _passwordService = passwordService;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public string GenerateToken()
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        var token = new char[10];

        using (var rng = RandomNumberGenerator.Create())
        {
            var bytes = new byte[10];
            rng.GetBytes(bytes);

            for (int i = 0; i < 10; i++)
            {
                token[i] = chars[bytes[i] % chars.Length];
            }
        }

        return new string(token);
    }

    public async Task<string> CreateMobileAccessAsync(Guid teamMemberId)
    {
        var member = await _db.TeamMembers.FindAsync(teamMemberId);
        if (member == null)
            throw new InvalidOperationException("Mitarbeiter nicht gefunden");

        // Generiere eindeutigen Token
        string token;
        do
        {
            token = GenerateToken();
        } while (await _db.TeamMembers.AnyAsync(m => m.MobileToken == token));

        member.MobileToken = token;
        member.PinHash = null;
        member.MobilePinSet = false;
        member.MobilePinFailedAttempts = 0;
        member.MobileTokenLocked = false;
        member.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Mobile access created for team member {MemberId} with token {Token}",
            teamMemberId, token);

        return token;
    }

    public async Task<TeamMember?> ValidateTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        return await _db.TeamMembers
            .FirstOrDefaultAsync(m => m.MobileToken == token && m.IsActive);
    }

    public async Task SetPinAsync(string token, string pin)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Ungültiger Token");

        if (!IsValidPin(pin))
            throw new InvalidOperationException("PIN muss genau 4 Ziffern enthalten");

        if (IsPinTooSimple(pin))
            throw new InvalidOperationException("PIN ist zu einfach. Bitte wähle eine andere Kombination");

        var member = await _db.TeamMembers
            .FirstOrDefaultAsync(m => m.MobileToken == token && m.IsActive);

        if (member == null)
            throw new InvalidOperationException("Ungültiger Token");

        member.PinHash = _passwordService.HashPassword(pin);
        member.MobilePinSet = true;
        member.MobilePinFailedAttempts = 0;
        member.MobileTokenLocked = false;
        member.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("PIN set for mobile token {Token}", token);
    }

    public async Task<MobilePinResult> VerifyPinAsync(string token, string pin)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(pin))
            return new MobilePinResult { Success = false };

        var member = await _db.TeamMembers
            .FirstOrDefaultAsync(m => m.MobileToken == token && m.IsActive);

        if (member == null)
        {
            _logger.LogWarning("Mobile token verification failed: token not found {Token}", token);
            return new MobilePinResult { Success = false };
        }

        if (member.MobileTokenLocked)
        {
            _logger.LogWarning("Mobile token verification failed: token locked {Token}", token);
            return new MobilePinResult { Success = false, Locked = true };
        }

        if (string.IsNullOrEmpty(member.PinHash))
        {
            _logger.LogWarning("Mobile token verification failed: no PIN set {Token}", token);
            return new MobilePinResult { Success = false };
        }

        var isValid = _passwordService.VerifyPassword(member.PinHash, pin);

        if (!isValid)
        {
            member.MobilePinFailedAttempts++;

            if (member.MobilePinFailedAttempts >= 3)
            {
                member.MobileTokenLocked = true;
                await _db.SaveChangesAsync();

                _logger.LogWarning("Mobile token locked after 3 failed attempts {Token}", token);

                return new MobilePinResult
                {
                    Success = false,
                    Locked = true,
                    RemainingAttempts = 0
                };
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation("Mobile PIN verification failed for {Token}, {Attempts} attempts",
                token, member.MobilePinFailedAttempts);

            return new MobilePinResult
            {
                Success = false,
                RemainingAttempts = 3 - member.MobilePinFailedAttempts
            };
        }

        // Erfolg: Fehlversuche zurücksetzen
        member.MobilePinFailedAttempts = 0;
        await _db.SaveChangesAsync();

        // JWT Token generieren
        var jwtToken = _jwtTokenService.GenerateToken(member);

        _logger.LogInformation("Mobile PIN verified successfully for {Token}", token);

        return new MobilePinResult
        {
            Success = true,
            User = member,
            JwtToken = jwtToken
        };
    }

    public async Task ResetMobileAccessAsync(Guid teamMemberId)
    {
        var member = await _db.TeamMembers.FindAsync(teamMemberId);
        if (member == null)
            throw new InvalidOperationException("Mitarbeiter nicht gefunden");

        // Generiere neuen Token
        string token;
        do
        {
            token = GenerateToken();
        } while (await _db.TeamMembers.AnyAsync(m => m.MobileToken == token));

        member.MobileToken = token;
        member.PinHash = null;
        member.MobilePinSet = false;
        member.MobilePinFailedAttempts = 0;
        member.MobileTokenLocked = false;
        member.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Mobile access reset for team member {MemberId} with new token {Token}",
            teamMemberId, token);
    }

    public async Task UnlockMobileAccessAsync(Guid teamMemberId)
    {
        var member = await _db.TeamMembers.FindAsync(teamMemberId);
        if (member == null)
            throw new InvalidOperationException("Mitarbeiter nicht gefunden");

        member.MobileTokenLocked = false;
        member.MobilePinFailedAttempts = 0;
        member.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Mobile access unlocked for team member {MemberId}", teamMemberId);
    }

    private bool IsValidPin(string pin)
    {
        return !string.IsNullOrWhiteSpace(pin) &&
               pin.Length == 4 &&
               pin.All(char.IsDigit);
    }

    private bool IsPinTooSimple(string pin)
    {
        return ForbiddenPins.Contains(pin);
    }
}
