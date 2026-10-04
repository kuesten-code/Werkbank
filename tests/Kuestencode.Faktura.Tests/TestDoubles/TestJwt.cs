using System.Text;

namespace Kuestencode.Faktura.Tests.TestDoubles;

/// <summary>
/// Erzeugt ein unsigniertes, aber syntaktisch gültiges JWT. JwtPrincipalParser (Kuestencode.Shared.UI)
/// validiert nur Struktur/Ablaufzeit, keine Signatur — analog dazu, wie JwtUserContextMiddleware einem
/// bereits vom Host geprüften Token vertraut. Reicht für [RequireRole] in Controller-Tests.
/// </summary>
public static class TestJwt
{
    public static string Create(string role)
    {
        static string Base64Url(string json) => Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        var header = Base64Url("""{"alg":"none","typ":"JWT"}""");
        var exp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        var payload = Base64Url($$"""
            {"http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier":"{{Guid.NewGuid()}}","http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name":"Test {{role}}","http://schemas.microsoft.com/ws/2008/06/identity/claims/role":"{{role}}","exp":{{exp}}}
            """);

        return $"{header}.{payload}.unsigned";
    }
}
