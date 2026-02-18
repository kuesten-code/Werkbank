using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Kuestencode.Shared.Contracts.Navigation;
using Kuestencode.Shared.UI.Services;
using Kuestencode.Shared.UI.Middleware;

namespace Kuestencode.Shared.UI.Extensions;

public static class ModuleHealthMonitorExtensions
{
    /// <summary>
    /// Adds Module Health Monitor to the service collection.
    /// The monitor tracks health check requests and re-registers with the Host if no health check
    /// is received within 60 seconds.
    /// </summary>
    public static IServiceCollection AddModuleHealthMonitor(
        this IServiceCollection services,
        string moduleName,
        Func<IConfiguration, ModuleInfoDto> getModuleInfo,
        IConfiguration configuration)
    {
        services.AddSingleton(sp => new ModuleHealthMonitor(
            sp.GetRequiredService<ILogger<ModuleHealthMonitor>>(),
            configuration,
            moduleName,
            () => getModuleInfo(configuration)
        ));

        services.AddHostedService(sp => sp.GetRequiredService<ModuleHealthMonitor>());

        return services;
    }

    /// <summary>
    /// Adds the Health Check Tracker middleware to the application pipeline.
    /// This middleware records when health checks are received.
    /// </summary>
    public static IApplicationBuilder UseModuleHealthMonitor(this IApplicationBuilder app)
    {
        return app.UseHealthCheckTracker();
    }
}
