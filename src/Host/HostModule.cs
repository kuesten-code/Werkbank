using Kuestencode.Core.Interfaces;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Data.Repositories;
using Kuestencode.Werkbank.Host.Services;
using Kuestencode.Werkbank.Host.Services.Email;
using Kuestencode.Werkbank.Host.Services.Pdf;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Host;

/// <summary>
/// Extension Methods für Host-Service-Registrierung.
/// </summary>
public static class HostModule
{
    /// <summary>
    /// Registriert alle Host-Services (Company, Customer, Email, PDF).
    /// </summary>
    public static IServiceCollection AddHostServices(this IServiceCollection services, IConfiguration configuration)
    {
        // DbContext
        services.AddDbContext<HostDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped<ICustomerRepository, CustomerRepository>();

        // Services
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddSingleton<PasswordEncryptionService>();
        services.AddScoped<ITeamMemberService, TeamMemberService>();

        // Engines
        services.AddScoped<IEmailEngine, EmailEngine>();
        services.AddScoped<IPdfEngine, PdfEngine>();

        // Auch das alte IEmailService für Abwärtskompatibilität
        // (CoreEmailService aus Core wird nicht mehr benötigt, EmailEngine ersetzt ihn)

        return services;
    }

    /// <summary>
    /// Wendet Datenbankmigrationen an.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var hostContext = scope.ServiceProvider.GetRequiredService<HostDbContext>();

        await hostContext.Database.MigrateAsync();
    }
}
