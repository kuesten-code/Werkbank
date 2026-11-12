using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Kuestencode.Shared.UI.Auth;
using Microsoft.IdentityModel.Tokens;

namespace Kuestencode.Faktura.Tests.TestDoubles;

/// <summary>
/// Erzeugt ein JWT wie der Host es ausstellt. Die Module prüfen die Signatur, daher muss
/// <see cref="Secret"/> mit dem in <see cref="FakturaWebApplicationFactory"/> konfigurierten übereinstimmen.
/// </summary>
public static class TestJwt
{
    public const string Secret = "faktura-tests-jwt-secret-0123456789abcdef";

    public static string Create(string role, string secret = Secret)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, $"Test {role}"),
            new Claim(ClaimTypes.Role, role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var token = new JwtSecurityToken(
            issuer: WerkbankJwt.DefaultIssuer,
            audience: WerkbankJwt.DefaultIssuer,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
