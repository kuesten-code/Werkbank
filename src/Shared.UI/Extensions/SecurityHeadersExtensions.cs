using Microsoft.AspNetCore.Builder;

namespace Kuestencode.Shared.UI.Extensions;

public static class SecurityHeadersExtensions
{
    /// <summary>
    /// Gleiche Schutz-Header in Host und Modulen, damit auch bei direktem Modulzugriff kein MIME-Sniffing
    /// (z. B. hochgeladene Datei als HTML) und kein Einbetten in fremde Seiten möglich ist.
    /// Gesetzt erst kurz vor dem Senden: so überschreibt der Host die vom Modul durchgereichten Werte,
    /// statt sie über den Proxy ein zweites Mal anzuhängen.
    /// </summary>
    public static IApplicationBuilder UseWerkbankSecurityHeaders(this IApplicationBuilder app) =>
        app.Use((context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers.XContentTypeOptions = "nosniff";
                headers.XFrameOptions = "SAMEORIGIN";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                return Task.CompletedTask;
            });
            return next(context);
        });
}
