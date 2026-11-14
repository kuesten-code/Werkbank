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

    private const int AttemptsPerLockout = 3;
    private static readonly TimeSpan FirstLockout = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan MaxLockout = TimeSpan.FromHours(24);

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

        // Sperre abgelaufen: Zähler bleibt, damit die nächste Sperre länger dauert
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

    /// <summary>
    /// Ein falscher zweiter Faktor zählt wie ein falsches Passwort, sonst ließe sich der Code beliebig durchprobieren.
    /// </summary>
    public Task<AuthResult> RecordFailedSecondFactorAsync(TeamMember member) => HandleFailedLoginAsync(member);

    /// <summary>
    /// Nach je drei Fehlversuchen eine befristete Sperre, die sich jedes Mal verdoppelt (höchstens 24 h).
    /// Bewusst nie dauerhaft: sonst könnte jeder, der eine Mail-Adresse kennt, das Konto (auch eines Admins)
    /// so lange stilllegen, bis ein Admin es entsperrt.
    /// </summary>
    private async Task<AuthResult> HandleFailedLoginAsync(TeamMember member)
    {
        member.FailedLoginAttempts++;
        var remaining = AttemptsPerLockout - member.FailedLoginAttempts % AttemptsPerLockout;

        if (remaining < AttemptsPerLockout)
        {
            await _context.SaveChangesAsync();
            return new AuthResult
            {
                Success = false,
                Error = $"E-Mail oder Passwort falsch. Noch {remaining} Versuche.",
                RemainingAttempts = remaining
            };
        }

        var lockout = LockoutDuration(member.FailedLoginAttempts / AttemptsPerLockout);
        member.LockoutUntil = DateTime.UtcNow.Add(lockout);
        await _context.SaveChangesAsync();

        _logger.LogWarning("Temporäre Sperre für {Email} nach {Attempts} Fehlversuchen: {Minutes} Minuten",
            member.Email, member.FailedLoginAttempts, lockout.TotalMinutes);

        var minutes = (int)lockout.TotalMinutes;
        return new AuthResult
        {
            Success = false,
            Error = $"Account für {minutes} Minuten gesperrt. Bitte warten.",
            LockedForMinutes = minutes
        };
    }

    public static TimeSpan LockoutDuration(int lockoutNumber)
    {
        var factor = 1L << Math.Clamp(lockoutNumber - 1, 0, 16);
        return TimeSpan.FromTicks(Math.Min(FirstLockout.Ticks * factor, MaxLockout.Ticks));
    }
}
