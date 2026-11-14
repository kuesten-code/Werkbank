using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Kuestencode.Werkbank.Host.Auth;

/// <summary>
/// Begrenzt anonyme Anmelde-Endpunkte (Passwort, MFA-Code, PIN, Passwort-Reset) pro Client. Ergänzt die Sperre
/// pro Konto: ohne sie ließe sich ein 6-stelliger MFA-Code im 5-Minuten-Fenster durchprobieren.
/// </summary>
public static class AuthRateLimit
{
    public const string PolicyName = "auth";
    public const int PermitsPerMinute = 10;

    public static void Configure(RateLimiterOptions options)
    {
        options.AddPolicy(PolicyName, context => RateLimitPartition.GetFixedWindowLimiter(
            ClientKey(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = PermitsPerMinute,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.HttpContext.Response.WriteAsJsonAsync(
                new { error = "Zu viele Versuche. Bitte eine Minute warten." }, cancellationToken);
        };
    }

    /// <summary>
    /// Hinter dem Reverse-Proxy sähe der Host sonst nur dessen IP, und alle Nutzer teilten sich ein Kontingent.
    /// Ein gefälschter Header verschafft einem Angreifer nur ein eigenes Kontingent; die Sperre pro Konto bleibt.
    /// </summary>
    public static string ClientKey(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
        var firstHop = forwardedFor.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
        return firstHop ?? context.Connection.RemoteIpAddress?.ToString() ?? "unbekannt";
    }
}
