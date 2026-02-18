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

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(TeamMember member)
    {
        var secret = GetOrGenerateJwtSecret();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, member.Id.ToString()),
            new Claim(ClaimTypes.Name, member.DisplayName),
            new Claim(ClaimTypes.Email, member.Email ?? ""),
            new Claim(ClaimTypes.Role, member.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: "KuestencodeWerkbank",
            audience: "KuestencodeWerkbank",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(30),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GetOrGenerateJwtSecret()
    {
        var secret = _configuration["Jwt:Secret"];

        if (string.IsNullOrEmpty(secret))
        {
            secret = GenerateRandomSecret();
        }

        if (secret.Length < 32)
        {
            throw new InvalidOperationException("JWT Secret muss mindestens 32 Zeichen lang sein.");
        }

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
