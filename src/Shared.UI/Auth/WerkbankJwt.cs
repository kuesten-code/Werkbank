using System.Text;
using Microsoft.AspNetCore.Http;
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
    /// Der MFA-Zwischen-Token bekommt ein eigenes Audience, damit er nie als Session-Token
    /// durchgeht (und umgekehrt).
    /// </summary>
    public static string MfaAudience(string issuer) => issuer + "-MFA";

    public static TokenValidationParameters CreateValidationParameters(string secret, string issuer, string audience) => new()
    {
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
        ClockSkew = TimeSpan.FromMinutes(1)
    };

    public static string? ExtractToken(HttpContext httpContext)
    {
        if (httpContext.Request.Cookies.TryGetValue(CookieName, out var cookie)
            && !string.IsNullOrEmpty(cookie))
            return cookie;

        var authHeader = httpContext.Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return authHeader["Bearer ".Length..].Trim();

        return null;
    }
}
