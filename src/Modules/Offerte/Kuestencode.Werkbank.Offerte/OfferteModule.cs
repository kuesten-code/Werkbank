using Kuestencode.Werkbank.Offerte.Data;
using Kuestencode.Werkbank.Offerte.Data.Repositories;
using Kuestencode.Werkbank.Offerte.Data.Services;
using Kuestencode.Werkbank.Offerte.Domain.Interfaces;
using Kuestencode.Werkbank.Offerte.Domain.Services;
using Kuestencode.Werkbank.Offerte.Domain.Validation;
using Kuestencode.Werkbank.Offerte.Services;
using Kuestencode.Werkbank.Offerte.Services.Email;
using Kuestencode.Werkbank.Offerte.Services.Pdf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kuestencode.Werkbank.Offerte;

/// <summary>
/// Module definition for the Offerte (Angebot) module.
/// Provides extension methods for service registration.
/// </summary>
public static class OfferteModule
{
    /// <summary>
    /// Adds all Offerte module services to the service collection.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddOfferteModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext with PostgreSQL (Offerte-Schema)
        services.AddDbContext<OfferteDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Register Repositories
        services.AddScoped<IAngebotRepository, AngebotRepository>();

        // Register Domain Services
        services.AddScoped<AngebotStatusService>();
        services.AddScoped<AngebotValidator>();
        services.AddScoped<IAngebotsnummernService, AngebotsnummernService>();

        // Register Application Services
        services.AddScoped<IOffertePdfService, OffertePdfService>();
        services.AddScoped<IOfferteVersandService, OfferteVersandService>();
        services.AddScoped<IOfferteDruckService, OfferteDruckService>();
        services.AddScoped<IOfferteKopierService, OfferteKopierService>();
        services.AddScoped<IOfferteUeberfuehrungService, OfferteUeberfuehrungService>();
        services.AddScoped<IOfferteSettingsService, OfferteSettingsService>();
        services.AddScoped<IOffertePreviewService, OffertePreviewService>();

        // Register Email Services
        services.AddScoped<IOfferteEmailTemplateRenderer, OfferteEmailTemplateRenderer>();

        // Register Ablauf Service (für automatische Prüfung abgelaufener Angebote)
        services.AddScoped<IAngebotAblaufService, AngebotAblaufService>();

        return services;
    }

    /// <summary>
    /// Applies pending database migrations.
    /// </summary>
    public static async Task ApplyMigrationsAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OfferteDbContext>();
        await context.Database.MigrateAsync();
    }
}
