namespace Kuestencode.Werkbank.Host.Auth;

/// <summary>
/// Nach dem Login nur auf eigene Seiten weiterleiten. Ein fremdes Ziel im Link (returnUrl) würde Nutzer nach
/// einem echten Login auf eine Phishing-Seite schicken.
/// </summary>
public static class LocalReturnUrl
{
    public static string Sanitize(string? returnUrl) =>
        returnUrl != null
        && returnUrl.StartsWith('/')
        && !returnUrl.StartsWith("//")
        && !returnUrl.StartsWith("/\\")
        && !returnUrl.Any(char.IsControl)
            ? returnUrl
            : "/";
}
