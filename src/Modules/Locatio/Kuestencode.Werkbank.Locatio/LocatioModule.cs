using Kuestencode.Werkbank.Locatio.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Locatio;

/// <summary>
/// Module definition for the Locatio (Vermietungsverwaltung) module.
/// </summary>
public static class LocatioModule
{
    public static IServiceCollection AddLocatioModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Factory statt scoped DbContext: Blazor-Server-Circuits sind langlebig und
        // würden sonst parallele Operationen auf demselben Context auslösen.
        services.AddDbContextFactory<LocatioDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        return services;
    }

    public static async Task ApplyMigrationsAsync(IServiceProvider serviceProvider)
    {
        var factory = serviceProvider.GetRequiredService<IDbContextFactory<LocatioDbContext>>();
        await using var context = await factory.CreateDbContextAsync();
        await context.Database.MigrateAsync();
    }
}
