using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Kuestencode.Shared.UI.Services;

namespace Kuestencode.Shared.UI.Middleware;

/// <summary>
/// Middleware that tracks health check requests and notifies the ModuleHealthMonitor
/// </summary>
public class HealthCheckTrackerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ModuleHealthMonitor _healthMonitor;

    public HealthCheckTrackerMiddleware(RequestDelegate next, ModuleHealthMonitor healthMonitor)
    {
        _next = next;
        _healthMonitor = healthMonitor;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if this is a health check request
        if (context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase))
        {
            _healthMonitor.RecordHealthCheck();
        }

        await _next(context);
    }
}

public static class HealthCheckTrackerMiddlewareExtensions
{
    public static IApplicationBuilder UseHealthCheckTracker(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<HealthCheckTrackerMiddleware>();
    }
}
