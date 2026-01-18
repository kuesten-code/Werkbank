using Kuestencode.Core.Interfaces;
using Kuestencode.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Kuestencode.Core.Extensions;

/// <summary>
/// Extension methods for registering Core services with DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Kuestencode.Core services to the service collection.
    /// Note: ICompanyService must be registered by the consuming application
    /// since it requires database access.
    /// </summary>
    public static IServiceCollection AddKuestencodeCore(this IServiceCollection services)
    {
        // Register core email service
        services.AddScoped<IEmailService, CoreEmailService>();

        return services;
    }

    /// <summary>
    /// Adds Kuestencode.Core services with a custom ICompanyService implementation.
    /// </summary>
    public static IServiceCollection AddKuestencodeCore<TCompanyService>(this IServiceCollection services)
        where TCompanyService : class, ICompanyService
    {
        services.AddScoped<ICompanyService, TCompanyService>();
        services.AddScoped<IEmailService, CoreEmailService>();

        return services;
    }
}
