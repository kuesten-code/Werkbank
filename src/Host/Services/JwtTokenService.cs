using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Kuestencode.Shared.UI.Auth;
using Kuestencode.Werkbank.Host.Models;
using Microsoft.IdentityModel.Tokens;

namespace Kuestencode.Werkbank.Host.Services;

public interface IJwtTokenService
{
    string GenerateToken(TeamMember member);

    /// <summary>
    /// Kurzlebiger Zwischen-Token nach korrektem Passwort, solange der zweite Faktor aussteht.
    /// Trägt ein eigenes Audience und wird deshalb von <see cref="ValidateToken"/> abgelehnt.
    /// </summary>
    string GenerateMfaToken(TeamMember member);

    /// <summary>
    /// Validiert Signatur, Ablauf, Issuer und Audience eines Session-JWT.
    /// Einzige Stelle im Host, die das JWT-Secret zur Prüfung heranzieht
    /// (AuthMiddleware, AuthController und WerkbankAuthStateProvider nutzen diese Methode).
    /// </summary>
    ClaimsPrincipal? ValidateToken(string token);

    ClaimsPrincipal? ValidateMfaToken(string token);
}

public class JwtTokenService : IJwtTokenService
{
    private static readonly TimeSpan MfaTokenLifetime = TimeSpan.FromMinutes(5);

    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtTokenService> _logger;

    public JwtTokenService(IConfiguration configuration, ILogger<JwtTokenService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private string Issuer => _configuration["Jwt:Issuer"] ?? WerkbankJwt.DefaultIssuer;

    public string GenerateToken(TeamMember member)
    {
        var expiresInDays = _configuration.GetValue("Jwt:ExpiresInDays", 30);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, member.Id.ToString()),
            new Claim(ClaimTypes.Name, member.DisplayName),
            new Claim(ClaimTypes.Email, member.Email ?? ""),
            new Claim(ClaimTypes.Role, member.Role.ToString())
        };

        return WriteToken(claims, Issuer, TimeSpan.FromDays(expiresInDays));
    }

    public string GenerateMfaToken(TeamMember member)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, member.Id.ToString()) };
        return WriteToken(claims, WerkbankJwt.MfaAudience(Issuer), MfaTokenLifetime);
    }

    public ClaimsPrincipal? ValidateToken(string token) => Validate(token, Issuer);

    public ClaimsPrincipal? ValidateMfaToken(string token) => Validate(token, WerkbankJwt.MfaAudience(Issuer));

    private string WriteToken(IEnumerable<Claim> claims, string audience, TimeSpan lifetime)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetOrGenerateJwtSecret()));

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(lifetime),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private ClaimsPrincipal? Validate(string token, string audience)
    {
        try
        {
            var parameters = WerkbankJwt.CreateValidationParameters(GetOrGenerateJwtSecret(), Issuer, audience);
            return new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "JWT-Validierung fehlgeschlagen");
            return null;
        }
    }

    private string GetOrGenerateJwtSecret()
    {
        var secret = _configuration["Jwt:Secret"];

        if (!string.IsNullOrWhiteSpace(secret) && secret.Length >= WerkbankJwt.MinSecretLength)
            return secret;

        var filePath = Path.Combine(AppContext.BaseDirectory, "data", "jwt-secret.txt");
        var dir = Path.GetDirectoryName(filePath)!;
        Directory.CreateDirectory(dir);

        if (File.Exists(filePath))
        {
            var stored = File.ReadAllText(filePath).Trim();
            if (stored.Length >= WerkbankJwt.MinSecretLength)
                return stored;
        }

        secret = GenerateRandomSecret();
        File.WriteAllText(filePath, secret);
        _logger.LogInformation("JWT-Secret automatisch generiert und gespeichert (JwtTokenService).");

        return secret;
    }

    private string GenerateRandomSecret()
    {
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes);
    }
}
