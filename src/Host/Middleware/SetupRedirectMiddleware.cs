using Kuestencode.Werkbank.Host.Services;

namespace Kuestencode.Werkbank.Host.Middleware;

/// <summary>
/// Middleware that redirects all requests to /setup if initial setup is required
/// </summary>
public class SetupRedirectMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SetupRedirectMiddleware> _logger;

    // Paths that should not be redirected
    private static readonly HashSet<string> AllowedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/setup",
        "/api/setup",
        "/_blazor",
        "/_framework",
        "/css",
        "/js",
        "/_content"
    };

    public SetupRedirectMiddleware(RequestDelegate next, ILogger<SetupRedirectMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ISetupService setupService)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Check if the path should be allowed without setup
        if (ShouldAllowPath(path))
        {
            await _next(context);
            return;
        }

        // Check if setup is required
        bool setupRequired;
        try
        {
            setupRequired = await setupService.IsSetupRequiredAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if setup is required");
            // On error, assume setup is required to be safe
            setupRequired = true;
        }

        if (setupRequired)
        {
            // Redirect to setup page
            if (!path.Equals("/setup", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Setup required, redirecting {Path} to /setup", path);
                context.Response.Redirect("/setup");
                return;
            }
        }

        await _next(context);
    }

    private static bool ShouldAllowPath(string path)
    {
        // Allow exact matches and paths that start with allowed paths
        return AllowedPaths.Any(allowed =>
            path.Equals(allowed, StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(allowed, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Extension method for registering the SetupRedirectMiddleware
/// </summary>
public static class SetupRedirectMiddlewareExtensions
{
    public static IApplicationBuilder UseSetupRedirect(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SetupRedirectMiddleware>();
    }
}
