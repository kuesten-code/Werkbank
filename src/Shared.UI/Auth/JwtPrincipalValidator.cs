using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Kuestencode.Shared.UI.Auth;

/// <summary>
/// Prüft Werkbank-Tokens (Nutzer-Session oder Service) in den Modulen mit demselben Secret wie der Host.
/// Ohne konfiguriertes Secret kann ein Modul keine Signatur prüfen und darf deshalb nicht starten.
/// </summary>
public sealed class JwtPrincipalValidator
{
    private readonly TokenValidationParameters _parameters;
    private readonly JwtSecurityTokenHandler _handler = new();

    public JwtPrincipalValidator(IConfiguration configuration)
    {
        var issuer = WerkbankJwt.Issuer(configuration);
        _parameters = WerkbankJwt.CreateValidationParameters(
            WerkbankJwt.RequireSecret(configuration), issuer, WerkbankJwt.RequestAudiences(issuer));
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
