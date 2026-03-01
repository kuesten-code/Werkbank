using Kuestencode.Werkbank.Saldo.Data;
using Kuestencode.Werkbank.Saldo.Data.Repositories;
using Kuestencode.Werkbank.Saldo.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kuestencode.Werkbank.Saldo;

/// <summary>
/// Module definition for the Saldo (EÜR) module.
/// Provides extension methods for service registration.
/// </summary>
public static class SaldoModule
{
    public static IServiceCollection AddSaldoModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext with PostgreSQL (Saldo-Schema)
        services.AddDbContext<SaldoDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Register Repositories
        services.AddScoped<ISaldoSettingsRepository, SaldoSettingsRepository>();
        services.AddScoped<IKontoRepository, KontoRepository>();
        services.AddScoped<IKategorieKontoMappingRepository, KategorieKontoMappingRepository>();
        services.AddScoped<IExportLogRepository, ExportLogRepository>();

        // Register Application Services
        services.AddScoped<ISaldoSettingsService, SaldoSettingsService>();
        services.AddScoped<IKontoService, KontoService>();
        services.AddScoped<IEuerService, EuerService>();

        // Note: IReceptaDataService is registered via AddHttpClient in ProgramApi.cs

        return services;
    }

    public static async Task ApplyMigrationsAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SaldoDbContext>();
        await context.Database.MigrateAsync();
        await SaldoSeedData.SeedAsync(context);
    }
}
