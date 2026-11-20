using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Kuestencode.Shared.UI.Extensions;

public static class PositionskatalogExtensions
{
    /// <summary>
    /// Registriert Lerner und Altbestand-Abgleich für den Positionskatalog. Setzt einen registrierten IHostApiClient voraus.
    /// </summary>
    public static IServiceCollection AddPositionskatalog<TQuelle>(this IServiceCollection services)
        where TQuelle : class, IPositionskatalogQuelle
    {
        services.AddScoped<IPositionskatalogLerner, PositionskatalogLerner>();
        services.AddScoped<IPositionskatalogQuelle, TQuelle>();
        services.AddHostedService<PositionskatalogAbgleichService>();
        return services;
    }
}
