using Kuestencode.Werkbank.Locatio.Data;
using Kuestencode.Werkbank.Locatio.Services;
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

        services.AddScoped<IGebaeudeService, GebaeudeService>();
        services.AddScoped<IEinheitService, EinheitService>();
        services.AddScoped<IVeraeusserungService, VeraeusserungService>();
        services.AddScoped<IVeraeusserungsgewinnService, VeraeusserungsgewinnService>();
        services.AddScoped<IKategorieService, KategorieService>();
        services.AddScoped<IKostenService, KostenService>();
        services.AddScoped<IMieteingangService, MieteingangService>();
        services.AddScoped<INebenkostenabrechnungService, NebenkostenabrechnungService>();
        services.AddScoped<IDarlehenService, DarlehenService>();
        services.AddScoped<IAfaService, AfaService>();
        services.AddScoped<IUebergabeprotokollService, UebergabeprotokollService>();
        services.AddScoped<ISaldoBuchungService, SaldoBuchungService>();

        return services;
    }

    public static async Task ApplyMigrationsAsync(IServiceProvider serviceProvider)
    {
        var factory = serviceProvider.GetRequiredService<IDbContextFactory<LocatioDbContext>>();
        await using var context = await factory.CreateDbContextAsync();
        await context.Database.MigrateAsync();
    }
}
