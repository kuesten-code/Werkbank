using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Kuestencode.Shared.UI.Auth;

/// <summary>
/// Prüft Werkbank-Session-Tokens in den Modulen mit demselben Secret wie der Host.
/// Ohne konfiguriertes Secret kann ein Modul keine Signatur prüfen und darf deshalb nicht starten.
/// </summary>
public sealed class JwtPrincipalValidator
{
    private readonly TokenValidationParameters _parameters;
    private readonly JwtSecurityTokenHandler _handler = new();

    public JwtPrincipalValidator(IConfiguration configuration)
    {
        var secret = configuration["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < WerkbankJwt.MinSecretLength)
            throw new InvalidOperationException(
                $"Jwt:Secret fehlt oder ist kürzer als {WerkbankJwt.MinSecretLength} Zeichen. " +
                "Das Modul braucht dasselbe JWT-Secret wie der Host (Umgebungsvariable Jwt__Secret).");

        var issuer = configuration["Jwt:Issuer"] ?? WerkbankJwt.DefaultIssuer;
        _parameters = WerkbankJwt.CreateValidationParameters(secret, issuer, issuer);
    }

    public ClaimsPrincipal? Validate(string token)
    {
        try
        {
            return _handler.ValidateToken(token, _parameters, out _);
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
        {
            return null;
        }
    }
}
