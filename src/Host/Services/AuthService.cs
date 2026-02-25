using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Kuestencode.Werkbank.Host.Services;

public class AuthService : IAuthService
{
    private readonly HostDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    private const int FirstLockoutThreshold = 3;
    private const int PermanentLockoutThreshold = 6;
    private const int TempLockoutMinutes = 15;

    public AuthService(
        HostDbContext context,
        IPasswordService passwordService,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _configuration = configuration;
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

        var token = GenerateToken(member);

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

    public string GenerateToken(TeamMember member)
    {
        var secret = GetOrGenerateJwtSecret();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var issuer = _configuration["Jwt:Issuer"] ?? "KuestencodeWerkbank";
        var expiresInDays = _configuration.GetValue("Jwt:ExpiresInDays", 30);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, member.Id.ToString()),
            new Claim(ClaimTypes.Email, member.Email ?? ""),
            new Claim(ClaimTypes.Name, member.DisplayName),
            new Claim(ClaimTypes.Role, member.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: issuer,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(expiresInDays),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
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

    private string GetOrGenerateJwtSecret()
    {
        var secret = _configuration["Jwt:Secret"];

        if (!string.IsNullOrWhiteSpace(secret) && secret.Length >= 32)
            return secret;

        // Auto-generate und in Datei persistieren
        var generated = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        var filePath = Path.Combine(
            AppContext.BaseDirectory, "data", "jwt-secret.txt");

        var dir = Path.GetDirectoryName(filePath)!;
        Directory.CreateDirectory(dir);

        if (File.Exists(filePath))
        {
            var stored = File.ReadAllText(filePath).Trim();
            if (stored.Length >= 32)
                return stored;
        }

        File.WriteAllText(filePath, generated);
        _logger.LogInformation("JWT-Secret automatisch generiert und gespeichert.");
        return generated;
    }
}
