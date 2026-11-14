using Kuestencode.Shared.UI.Auth;

namespace Kuestencode.Werkbank.Host.Auth;

/// <summary>
/// Einzige Stelle, die das Session-Cookie setzt (Web-Login, MFA, Mobile-PIN), damit alle Wege dieselben
/// Schutz-Flags haben: nur per HTTPS, nicht per JavaScript lesbar.
/// </summary>
public static class AuthCookie
{
    public static void Append(HttpResponse response, string token, TimeSpan lifetime) =>
        response.Cookies.Append(WerkbankJwt.CookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.Add(lifetime)
        });

    public static void Delete(HttpResponse response) =>
        response.Cookies.Delete(WerkbankJwt.CookieName, new CookieOptions { Path = "/" });
}
