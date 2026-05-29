using Kuestencode.Werkbank.Contracta.Data;
using Kuestencode.Werkbank.Contracta.Data.Repositories;
using Kuestencode.Werkbank.Contracta.Domain.Interfaces;
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
        // Factory pattern avoids concurrency issues in Blazor Server
        services.AddDbContextFactory<ContractaDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<ContractaDbContext>>().CreateDbContext());

        // Repositories
        services.AddScoped<IWartungsvertragRepository, WartungsvertragRepository>();

        // Domain services
        services.AddScoped<VertragStatusService>();
        services.AddScoped<WartungsvertragValidator>();

        // Application services
        services.AddSingleton<IModulVerfuegbarkeit, ModulVerfuegbarkeit>();
        services.AddScoped<IFaelligkeitsService, FaelligkeitsService>();
        services.AddScoped<IVertragsnummernService, VertragsnummernService>();
        services.AddScoped<IRechnungserstellungService, RechnungserstellungService>();

        return services;
    }

    public static async Task ApplyMigrationsAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ContractaDbContext>>();
        await using var context = await factory.CreateDbContextAsync();
        await context.Database.MigrateAsync();
    }
}
