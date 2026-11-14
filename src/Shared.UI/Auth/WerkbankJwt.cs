using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Kuestencode.Shared.UI.Auth;

/// <summary>
/// Gemeinsame JWT-Konventionen von Host (Ausstellung) und Modulen (Prüfung), damit beide Seiten
/// zwingend dieselben Validierungsregeln anwenden.
/// </summary>
public static class WerkbankJwt
{
    public const string CookieName = "werkbank_auth_cookie";
    public const string DefaultIssuer = "KuestencodeWerkbank";
    public const int MinSecretLength = 32;

    /// <summary>
    /// Kennzeichnet Server-zu-Server-Tokens. Nur damit dürfen sich Module beim Host registrieren.
    /// </summary>
    public const string ServiceClaim = "werkbank_service";

    /// <summary>
    /// Der MFA-Zwischen-Token bekommt ein eigenes Audience, damit er nie als Session-Token
    /// durchgeht (und umgekehrt).
    /// </summary>
    public static string MfaAudience(string issuer) => issuer + "-MFA";

    public static string ServiceAudience(string issuer) => issuer + "-Service";

    /// <summary>
    /// Audiences, die Host und Module als Ausweis für einen Request akzeptieren: Nutzer-Sessions
    /// und Server-zu-Server-Aufrufe, nie aber den MFA-Zwischen-Token.
    /// </summary>
    public static string[] RequestAudiences(string issuer) => [issuer, ServiceAudience(issuer)];

    public static string Issuer(IConfiguration configuration) => configuration["Jwt:Issuer"] ?? DefaultIssuer;

    /// <summary>
    /// Beispielwerte aus Anleitung und .env-Vorlage: wer sie übernimmt, signiert mit einem öffentlich bekannten
    /// Schlüssel, und jeder könnte sich Admin-Tokens ausstellen.
    /// </summary>
    private static readonly HashSet<string> KnownPlaceholders = new(StringComparer.Ordinal)
    {
        "abcdefghijklmnopqrstuvwxyz123456",
        "change_me_use_a_long_random_string_here"
    };

    public static string RequireSecret(IConfiguration configuration)
    {
        var secret = configuration["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < MinSecretLength)
            throw new InvalidOperationException(
                $"Jwt:Secret fehlt oder ist kürzer als {MinSecretLength} Zeichen. " +
                "Host und Module brauchen dasselbe JWT-Secret (Umgebungsvariable Jwt__Secret).");
        if (KnownPlaceholders.Contains(secret))
            throw new InvalidOperationException(
                "Jwt:Secret ist ein bekannter Beispielwert. Bitte ein eigenes Secret erzeugen: openssl rand -base64 32");
        return secret;
    }

    public static SymmetricSecurityKey SigningKey(string secret) => new(Encoding.UTF8.GetBytes(secret));

    public static TokenValidationParameters CreateValidationParameters(string secret, string issuer, params string[] audiences) => new()
    {
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudiences = audiences,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = SigningKey(secret),
        ClockSkew = TimeSpan.FromMinutes(1)
    };

    /// <summary>
    /// Header vor Cookie, wie im Host: der Host-Proxy setzt den Header auf das Token, das er selbst
    /// geprüft bzw. bei deaktivierter Anmeldung ausgestellt hat. Ein veraltetes Cookie darf das nicht überstimmen.
    /// </summary>
    public static string? ExtractToken(HttpContext httpContext)
    {
        var authHeader = httpContext.Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return authHeader["Bearer ".Length..].Trim();

        if (httpContext.Request.Cookies.TryGetValue(CookieName, out var cookie) && !string.IsNullOrEmpty(cookie))
            return cookie;

        return null;
    }
}
