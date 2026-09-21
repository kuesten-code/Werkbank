using Kuestencode.Core.Interfaces;
using Kuestencode.Werkbank.Host.Auth;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Data.Repositories;
using Kuestencode.Werkbank.Host.Services;
using Kuestencode.Werkbank.Host.Services.Backup;
using Kuestencode.Werkbank.Host.Services.Docker;
using Kuestencode.Werkbank.Host.Services.Email;
using Kuestencode.Werkbank.Host.Services.Pdf;
using Microsoft.AspNetCore.Components.Authorization;
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
        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddScoped<ITeamMemberService, TeamMemberService>();
        services.AddScoped<IInviteService, InviteService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IWerkbankSettingsService, WerkbankSettingsService>();
        services.AddScoped<INumberFormatSettingsService, NumberFormatSettingsService>();
        services.AddScoped<ISetupService, SetupService>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IMobileTokenService, MobileTokenService>();
        services.AddScoped<IMobileRapportService, MobileRapportService>();
        services.AddScoped<ITotpService, TotpService>();

        // Backup
        services.AddSingleton<IBackupTargetProviderFactory, BackupTargetProviderFactory>();
        services.AddSingleton<IBackupScheduleChangeSignal, BackupScheduleChangeSignal>();
        services.AddScoped<IBackupService, BackupService>();
        services.AddHostedService<BackupSchedulerService>();

        // Docker-Steuerung (über den Socket-Proxy "docker-control", siehe docker-compose.yml)
        services.AddHttpClient<IDockerControlService, DockerControlService>(client =>
        {
            client.BaseAddress = new Uri(configuration["DockerControl:BaseUrl"] ?? "http://docker-control:2375");
            client.Timeout = TimeSpan.FromMinutes(2);
        });

        services.AddSingleton<IManuallyStoppedModules>(_ =>
            new ManuallyStoppedModules(Path.Combine(AppContext.BaseDirectory, "data", "manually-stopped-modules.json")));
        services.AddScoped<IModuleControlService, ModuleControlService>();
        services.AddScoped<IStackControlService, StackControlService>();

        // HTTP Clients
        services.AddHttpClient();

        // Auth
        services.AddScoped<WerkbankAuthStateProvider>();
        services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<WerkbankAuthStateProvider>());

        // Engines
        services.AddScoped<IEmailEngine, EmailEngine>();
        services.AddScoped<IPdfEngine, PdfEngine>();

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
