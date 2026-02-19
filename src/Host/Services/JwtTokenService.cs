using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Kuestencode.Werkbank.Host.Models;
using Microsoft.IdentityModel.Tokens;

namespace Kuestencode.Werkbank.Host.Services;

public interface IJwtTokenService
{
    string GenerateToken(TeamMember member);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtTokenService> _logger;

    public JwtTokenService(IConfiguration configuration, ILogger<JwtTokenService> logger)
    {
        _configuration = configuration;
        _logger = logger;
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
            new Claim(ClaimTypes.Name, member.DisplayName),
            new Claim(ClaimTypes.Email, member.Email ?? ""),
            new Claim(ClaimTypes.Role, member.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: issuer,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(expiresInDays),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GetOrGenerateJwtSecret()
    {
        var secret = _configuration["Jwt:Secret"];

        if (!string.IsNullOrWhiteSpace(secret) && secret.Length >= 32)
            return secret;

        var filePath = Path.Combine(AppContext.BaseDirectory, "data", "jwt-secret.txt");
        var dir = Path.GetDirectoryName(filePath)!;
        Directory.CreateDirectory(dir);

        if (File.Exists(filePath))
        {
            var stored = File.ReadAllText(filePath).Trim();
            if (stored.Length >= 32)
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
