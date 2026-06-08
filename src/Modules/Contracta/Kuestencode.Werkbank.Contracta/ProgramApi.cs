using System.Globalization;
using Kuestencode.Core.Interfaces;
using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.Contracts.Host;
using Kuestencode.Shared.Contracts.Navigation;
using Kuestencode.Shared.UI.Extensions;
using Kuestencode.Werkbank.Contracta.Services;
using Microsoft.AspNetCore.DataProtection;
using MudBlazor.Services;

namespace Kuestencode.Werkbank.Contracta;

public class ProgramApi
{
    public static async Task Main(string[] args)
    {
        var culture = new CultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseStaticWebAssets();
        builder.Configuration.AddJsonFile("appsettings.api.json", optional: true, reloadOnChange: true);
        builder.Configuration.AddEnvironmentVariables();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddAuthorization();

        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();

        builder.Services.AddScoped<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider,
            Kuestencode.Shared.UI.Auth.PassThroughAuthStateProvider>();

        builder.Services.AddMudServices();
        builder.Services.AddSingleton<MudBlazor.MudLocalizer, Kuestencode.Shared.UI.GermanMudLocalizer>();

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "Contracta API",
                Version = "v1",
                Description = "Wartungsverträge API for Kuestencode Werkbank"
            });
        });

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
            });
        });

        var keysDirectory = Path.Combine(builder.Environment.ContentRootPath, "data", "keys");
        Directory.CreateDirectory(keysDirectory);
        builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keysDirectory))
            .SetApplicationName("Kuestencode.Werkbank.Contracta");

        builder.Services.AddSingleton<IModuleRegistry, ApiModuleRegistry>();

        builder.Services.AddTransient<Kuestencode.Shared.UI.Handlers.AuthTokenDelegatingHandler>();

        builder.Services.AddHttpClient<IHostApiClient, HostApiClient>(client =>
        {
            var hostUrl = builder.Configuration.GetValue<string>("ServiceUrls:Host") ?? "http://localhost:8080";
            client.BaseAddress = new Uri(hostUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddHttpMessageHandler<Kuestencode.Shared.UI.Handlers.AuthTokenDelegatingHandler>();

        builder.Services.AddHttpClient<IFakturaApiClient, FakturaApiClient>(client =>
        {
            var fakturaUrl = builder.Configuration.GetValue<string>("ServiceUrls:Faktura") ?? "http://localhost:8081";
            client.BaseAddress = new Uri(fakturaUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddHttpMessageHandler<Kuestencode.Shared.UI.Handlers.AuthTokenDelegatingHandler>();

        builder.Services.AddTransient<ModuleAvailabilityService>();

        builder.Services.AddScoped<ICompanyService, ApiCompanyService>();
        builder.Services.AddScoped<ICustomerService, ApiCustomerService>();

        builder.Services.AddContractaModule(builder.Configuration);

        builder.Services.AddModuleHealthMonitor("Contracta", GetModuleInfo, builder.Configuration);

        var app = builder.Build();

        var applyMigrationsSetting = builder.Configuration["APPLY_MIGRATIONS"];
        var applyMigrations = string.IsNullOrWhiteSpace(applyMigrationsSetting) ||
                              !string.Equals(applyMigrationsSetting, "false", StringComparison.OrdinalIgnoreCase);

        var migrationLogger = app.Services.GetRequiredService<ILogger<ProgramApi>>();
        migrationLogger.LogInformation("APPLY_MIGRATIONS='{Setting}' => ApplyMigrations={Apply}",
            applyMigrationsSetting ?? "<null>", applyMigrations);

        if (applyMigrations)
        {
            try
            {
                migrationLogger.LogInformation("Applying Contracta database migrations...");
                await ContractaModule.ApplyMigrationsAsync(app.Services);
                migrationLogger.LogInformation("Contracta database migrations applied successfully.");
            }
            catch (Exception ex)
            {
                migrationLogger.LogError(ex, "An error occurred while applying Contracta migrations.");
                throw;
            }
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Contracta API v1");
            });
        }

        app.UsePathBase("/contracta");

        app.UseCors();
        app.UseStaticFiles();

        app.UseModuleHealthMonitor();

        app.UseRouting();
        app.UseAuthorization();

        app.MapRazorPages();
        app.MapControllers();

        app.MapGet("/health", () => Results.Ok(new { status = "healthy", module = "Contracta" }));

        app.MapBlazorHub("/_blazor");
        app.MapFallbackToPage("/{*path:nonfile}", "/_Host");
        app.MapFallbackToPage("/", "/_Host");

        _ = Task.Run(async () =>
        {
            await Task.Delay(5000);
            await RegisterWithHost(app.Configuration, app.Services.GetRequiredService<ILogger<ProgramApi>>());
        });

        app.Run();
    }

    private static ModuleInfoDto GetModuleInfo(IConfiguration config)
    {
        var selfUrl = config.GetValue<string>("ServiceUrls:Self") ?? "http://localhost:8087";
        var moduleVersion = config["MODULE_VERSION"]
            ?? config["IMAGE_TAG"]
            ?? config["DOCKER_IMAGE_TAG"]
            ?? "dev";

        return new ModuleInfoDto
        {
            ModuleName = "Contracta",
            DisplayName = "Contracta",
            Version = moduleVersion,
            LogoUrl = "/contracta/company/logos/Contracta_Logo.png",
            HealthCheckUrl = $"{selfUrl}/contracta/health",
            NavigationItems = new List<NavItemDto>
            {
                new NavItemDto
                {
                    Label = "Contracta",
                    Href = "/contracta/vertraege",
                    Icon = "/contracta/company/logos/Contracta_Logo.png",
                    Type = NavItemType.Link,
                    AllowedRoles = new List<UserRole> { UserRole.Buero, UserRole.Admin }
                },
                new NavItemDto
                {
                    Label = "Verträge",
                    Href = "/contracta/vertraege",
                    Icon = "",
                    Type = NavItemType.Link,
                    AllowedRoles = new List<UserRole> { UserRole.Buero, UserRole.Admin }
                }
            }
        };
    }

    private static async Task RegisterWithHost(IConfiguration config, ILogger logger)
    {
        try
        {
            var hostUrl = config.GetValue<string>("ServiceUrls:Host") ?? "http://localhost:8080";
            using var client = new HttpClient { BaseAddress = new Uri(hostUrl) };

            var moduleInfo = GetModuleInfo(config);
            var response = await client.PostAsJsonAsync("/api/modules/register", moduleInfo);

            if (response.IsSuccessStatusCode)
                logger.LogInformation("Contracta module registered successfully with Host at {HostUrl}", hostUrl);
            else
                logger.LogWarning("Failed to register Contracta module with Host. Status: {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error registering Contracta module with Host");
        }
    }
}
