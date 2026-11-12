using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Kuestencode.Shared.Contracts.Navigation;
using Kuestencode.Shared.UI.Services;
using Kuestencode.Shared.UI.Handlers;
using Kuestencode.Shared.UI.Middleware;

namespace Kuestencode.Shared.UI.Extensions;

public static class ModuleHealthMonitorExtensions
{
    /// <summary>
    /// Registriert das Modul beim Host (mit Service-Token) und überwacht die Health-Checks des Hosts.
    /// </summary>
    public static IServiceCollection AddModuleHealthMonitor(
        this IServiceCollection services,
        string moduleName,
        Func<IConfiguration, ModuleInfoDto> getModuleInfo,
        IConfiguration configuration)
    {
        services.AddHttpClient(ModuleHealthMonitor.HostClientName, client =>
        {
            client.BaseAddress = new Uri(configuration.GetValue<string>("ServiceUrls:Host") ?? "http://localhost:8080");
            client.Timeout = TimeSpan.FromSeconds(10);
        }).AddHttpMessageHandler<AuthTokenDelegatingHandler>();

        services.AddSingleton(sp => new ModuleHealthMonitor(
            sp.GetRequiredService<ILogger<ModuleHealthMonitor>>(),
            sp.GetRequiredService<IHttpClientFactory>(),
            moduleName,
            () => getModuleInfo(configuration)));

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
