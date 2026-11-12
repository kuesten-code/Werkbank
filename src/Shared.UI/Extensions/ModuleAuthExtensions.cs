using Kuestencode.Shared.UI.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Kuestencode.Shared.UI.Extensions;

public static class ModuleAuthExtensions
{
    public static IServiceCollection AddWerkbankModuleAuth(this IServiceCollection services)
    {
        services.AddSingleton<JwtPrincipalValidator>();
        services.AddScoped<AuthenticationStateProvider, PassThroughAuthStateProvider>();
        return services;
    }
}
