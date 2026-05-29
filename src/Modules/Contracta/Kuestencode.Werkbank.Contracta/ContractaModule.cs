using Kuestencode.Werkbank.Contracta.Data;
using Kuestencode.Werkbank.Contracta.Domain.Services;
using Kuestencode.Werkbank.Contracta.Services;
using Kuestencode.Werkbank.Contracta.Services.Implementation;
using Kuestencode.Werkbank.Contracta.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kuestencode.Werkbank.Contracta;

public static class ContractaModule
{
    public static IServiceCollection AddContractaModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ContractaDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddSingleton<IModulVerfuegbarkeit, ModulVerfuegbarkeit>();
        services.AddScoped<VertragStatusService>();
        services.AddScoped<WartungsvertragValidator>();

        return services;
    }

    public static async Task ApplyMigrationsAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ContractaDbContext>();
        await context.Database.MigrateAsync();
    }
}
