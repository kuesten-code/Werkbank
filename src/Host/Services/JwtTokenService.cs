using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
    /// Token für den impliziten Admin bei deaktivierter Anmeldung. Der Host-Proxy reicht es an die Module
    /// weiter, damit diese immer ein geprüftes Token verlangen können. Bewusst kurzlebig, damit es nach
    /// dem Einschalten der Anmeldung schnell wertlos wird.
    /// </summary>
    string GenerateAuthDisabledToken();

    /// <summary>
    /// Validiert Signatur, Ablauf, Issuer und Audience eines Session- oder Service-JWT.
    /// Einzige Stelle im Host, die das JWT-Secret zur Prüfung heranzieht
    /// (AuthMiddleware, AuthController und WerkbankAuthStateProvider nutzen diese Methode).
    /// </summary>
    ClaimsPrincipal? ValidateToken(string token);

    ClaimsPrincipal? ValidateMfaToken(string token);
}

public class JwtTokenService : IJwtTokenService
{
    private static readonly TimeSpan MfaTokenLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan AuthDisabledTokenLifetime = TimeSpan.FromMinutes(15);

    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtTokenService> _logger;
    private readonly string _secret;
    private readonly string _issuer;

    public JwtTokenService(IConfiguration configuration, ILogger<JwtTokenService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _secret = WerkbankJwt.RequireSecret(configuration);
        _issuer = WerkbankJwt.Issuer(configuration);
    }

    public string GenerateToken(TeamMember member)
    {
        var expiresInDays = _configuration.GetValue("Jwt:ExpiresInDays", 30);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, member.Id.ToString()),
            new Claim(ClaimTypes.Name, member.DisplayName),
            new Claim(ClaimTypes.Email, member.Email ?? ""),
            new Claim(ClaimTypes.Role, member.Role.ToString()),
            new Claim(SessionValidator.SessionVersionClaim, member.SessionVersion.ToString())
        };

        return WriteToken(claims, _issuer, TimeSpan.FromDays(expiresInDays));
    }

    public string GenerateMfaToken(TeamMember member)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, member.Id.ToString()) };
        return WriteToken(claims, WerkbankJwt.MfaAudience(_issuer), MfaTokenLifetime);
    }

    public string GenerateAuthDisabledToken()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.Empty.ToString()),
            new Claim(ClaimTypes.Name, "Admin"),
            new Claim(ClaimTypes.Role, UserRole.Admin.ToString())
        };
        return WriteToken(claims, _issuer, AuthDisabledTokenLifetime);
    }

    public ClaimsPrincipal? ValidateToken(string token) => Validate(token, WerkbankJwt.RequestAudiences(_issuer));

    public ClaimsPrincipal? ValidateMfaToken(string token) => Validate(token, WerkbankJwt.MfaAudience(_issuer));

    private string WriteToken(IEnumerable<Claim> claims, string audience, TimeSpan lifetime)
    {
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(lifetime),
            signingCredentials: new SigningCredentials(WerkbankJwt.SigningKey(_secret), SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private ClaimsPrincipal? Validate(string token, params string[] audiences)
    {
        try
        {
            var parameters = WerkbankJwt.CreateValidationParameters(_secret, _issuer, audiences);
            return new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _);
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
        {
            _logger.LogDebug(ex, "JWT-Validierung fehlgeschlagen");
            return null;
        }
    }
}
