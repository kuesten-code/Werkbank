using Kuestencode.Shared.UI.Auth;
using Kuestencode.Shared.UI.Handlers;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Kuestencode.Shared.UI.Extensions;

public static class ModuleAuthExtensions
{
    public static IServiceCollection AddWerkbankModuleAuth(this IServiceCollection services)
    {
        services.AddSingleton<JwtPrincipalValidator>();
        services.AddSingleton<ServiceTokenProvider>();
        services.AddTransient<AuthTokenDelegatingHandler>();
        services.AddScoped<AuthenticationStateProvider, PassThroughAuthStateProvider>();
        return services;
    }
}
