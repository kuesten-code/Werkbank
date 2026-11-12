using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Models;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Host.Services;

public class AuthService : IAuthService
{
    private readonly HostDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthService> _logger;

    private const int FirstLockoutThreshold = 3;
    private const int PermanentLockoutThreshold = 6;
    private const int TempLockoutMinutes = 15;

    public AuthService(
        HostDbContext context,
        IPasswordService passwordService,
        IJwtTokenService jwtTokenService,
        ILogger<AuthService> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var member = await _context.TeamMembers
            .FirstOrDefaultAsync(m => m.Email == email);

        if (member == null)
        {
            return new AuthResult
            {
                Success = false,
                Error = "E-Mail oder Passwort falsch.",
                RemainingAttempts = null
            };
        }

        if (!member.IsActive)
        {
            return new AuthResult { Success = false, Error = "Account deaktiviert." };
        }

        if (!member.HasCompletedSetup)
        {
            return new AuthResult { Success = false, Error = "Bitte zuerst Einladung annehmen." };
        }

        // Lockout prüfen
        if (member.IsLockedByAdmin)
        {
            return new AuthResult { Success = false, Error = "Account gesperrt. Bitte Administrator kontaktieren." };
        }

        if (member.LockoutUntil.HasValue && member.LockoutUntil > DateTime.UtcNow)
        {
            var remaining = (int)Math.Ceiling((member.LockoutUntil.Value - DateTime.UtcNow).TotalMinutes);
            return new AuthResult
            {
                Success = false,
                Error = $"Account für {remaining} Minuten gesperrt. Bitte warten.",
                LockedForMinutes = remaining
            };
        }

        // Temporäre Sperre abgelaufen? Zähler bleibt für zweite Runde
        if (member.LockoutUntil.HasValue && member.LockoutUntil <= DateTime.UtcNow)
        {
            member.LockoutUntil = null;
        }

        // Passwort prüfen
        if (string.IsNullOrEmpty(member.PasswordHash) ||
            !_passwordService.VerifyPassword(member.PasswordHash, password))
        {
            return await HandleFailedLoginAsync(member);
        }

        // Erfolg
        member.FailedLoginAttempts = 0;
        member.LockoutUntil = null;
        await _context.SaveChangesAsync();

        if (member.MfaEnabled)
        {
            _logger.LogInformation("MFA erforderlich für {Email}", email);
            return new AuthResult
            {
                Success = true,
                RequiresMfa = true,
                MfaToken = _jwtTokenService.GenerateMfaToken(member)
            };
        }

        var token = _jwtTokenService.GenerateToken(member);

        _logger.LogInformation("Erfolgreicher Login für {Email}", email);

        return new AuthResult
        {
            Success = true,
            Token = token,
            User = member
        };
    }

    public async Task<TeamMember?> GetCurrentUserAsync(string? userId)
    {
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var id))
            return null;

        return await _context.TeamMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);
    }

    private async Task<AuthResult> HandleFailedLoginAsync(TeamMember member)
    {
        member.FailedLoginAttempts++;

        var isSecondChance = member.FailedLoginAttempts > FirstLockoutThreshold;

        // Erste Runde: Fehlversuch 3 → 15 Min Sperre
        if (member.FailedLoginAttempts == FirstLockoutThreshold)
        {
            member.LockoutUntil = DateTime.UtcNow.AddMinutes(TempLockoutMinutes);
            await _context.SaveChangesAsync();

            _logger.LogWarning("Temporäre Sperre für {Email} nach {Attempts} Fehlversuchen",
                member.Email, member.FailedLoginAttempts);

            return new AuthResult
            {
                Success = false,
                Error = $"Account für {TempLockoutMinutes} Minuten gesperrt. Bitte warten.",
                LockedForMinutes = TempLockoutMinutes
            };
        }

        // Zweite Runde: Fehlversuch 6 → Permanent gesperrt
        if (member.FailedLoginAttempts >= PermanentLockoutThreshold)
        {
            member.IsLockedByAdmin = true;
            await _context.SaveChangesAsync();

            _logger.LogWarning("Permanente Sperre für {Email} nach {Attempts} Fehlversuchen",
                member.Email, member.FailedLoginAttempts);

            return new AuthResult
            {
                Success = false,
                Error = "Account gesperrt. Bitte Administrator kontaktieren."
            };
        }

        await _context.SaveChangesAsync();

        int maxAttempts = isSecondChance ? PermanentLockoutThreshold : FirstLockoutThreshold;
        int remaining = maxAttempts - member.FailedLoginAttempts;

        return new AuthResult
        {
            Success = false,
            Error = $"E-Mail oder Passwort falsch. Noch {remaining} Versuche.",
            RemainingAttempts = remaining
        };
    }
}
