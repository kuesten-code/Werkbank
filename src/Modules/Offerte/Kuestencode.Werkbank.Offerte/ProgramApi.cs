using System.Globalization;
using Kuestencode.Core.Interfaces;
using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.Contracts.Navigation;
using Kuestencode.Werkbank.Offerte.Services;
using Microsoft.AspNetCore.DataProtection;
using MudBlazor.Services;
using QuestPDF.Infrastructure;

namespace Kuestencode.Werkbank.Offerte;

/// <summary>
/// Entry point for Offerte when running as a standalone Blazor Server + API service.
/// This is used when Offerte runs in its own Docker container.
/// </summary>
public class ProgramApi
{
    public static async Task Main(string[] args)
    {
        // QuestPDF License configuration
        QuestPDF.Settings.License = LicenseType.Community;

        // Deutsche Lokalisierung für MudBlazor DatePicker etc.
        var culture = new CultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseStaticWebAssets();
        builder.Configuration.AddJsonFile("appsettings.api.json", optional: true, reloadOnChange: true);
        builder.Configuration.AddEnvironmentVariables();

        // Add Blazor Server + Razor Pages
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();

        // Add MudBlazor
        builder.Services.AddMudServices();

        // Add Controllers for API
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "Offerte API",
                Version = "v1",
                Description = "Angebotsmanagement API for Kuestencode Werkbank"
            });
        });

        // Add CORS for cross-origin requests from Host
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        // Add Data Protection for password encryption
        var keysDirectory = Path.Combine(builder.Environment.ContentRootPath, "data", "keys");
        Directory.CreateDirectory(keysDirectory);
        builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keysDirectory))
            .SetApplicationName("Kuestencode.Werkbank.Offerte");

        // Add Module Registry (stub for API mode - modules are registered via HTTP)
        builder.Services.AddSingleton<IModuleRegistry, ApiModuleRegistry>();

        // Add Host API Client (must be registered before API-based services)
        builder.Services.AddHttpClient<IHostApiClient, HostApiClient>(client =>
        {
            var hostUrl = builder.Configuration.GetValue<string>("ServiceUrls:Host") ?? "http://localhost:8080";
            client.BaseAddress = new Uri(hostUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Add API-based implementations of Host services (Company, Customer, Email)
        builder.Services.AddScoped<ICompanyService, ApiCompanyService>();
        builder.Services.AddScoped<ICustomerService, ApiCustomerService>();
        builder.Services.AddScoped<IEmailService, ApiEmailService>();

        // Add Offerte Services (includes DbContext, Repositories, etc.)
        builder.Services.AddOfferteModule(builder.Configuration);

        var app = builder.Build();

        // Apply migrations
        var applyMigrationsSetting = builder.Configuration["APPLY_MIGRATIONS"];
        var applyMigrations = string.IsNullOrWhiteSpace(applyMigrationsSetting) ||
                              !string.Equals(applyMigrationsSetting, "false", StringComparison.OrdinalIgnoreCase);

        var migrationLogger = app.Services.GetRequiredService<ILogger<ProgramApi>>();
        migrationLogger.LogInformation("APPLY_MIGRATIONS='{ApplyMigrationsSetting}' => ApplyMigrations={ApplyMigrations}",
            applyMigrationsSetting ?? "<null>", applyMigrations);

        if (applyMigrations)
        {
            try
            {
                migrationLogger.LogInformation("Applying Offerte database migrations...");
                await OfferteModule.ApplyMigrationsAsync(app.Services);
                migrationLogger.LogInformation("Offerte database migrations applied successfully.");
            }
            catch (Exception ex)
            {
                migrationLogger.LogError(ex, "An error occurred while applying Offerte migrations.");
                throw;
            }
        }

        // Configure pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Offerte API v1");
            });
        }

        // Set path base for reverse proxy compatibility
        app.UsePathBase("/offerte");

        app.UseCors();
        app.UseStaticFiles();
        app.UseRouting();

        // Map API Controllers
        app.MapControllers();

        // Health check endpoint for module registry
        app.MapGet("/health", () => Results.Ok(new { status = "healthy", module = "Offerte" }));

        // Map Blazor Hub (path relative to path base)
        app.MapBlazorHub("/_blazor");

        // Map Offerte pages (paths are relative to path base /offerte)
        app.MapFallbackToPage("/{*path:nonfile}", "/_Host");
        app.MapFallbackToPage("/", "/_Host");

        // Register with Host on startup
        _ = Task.Run(async () =>
        {
            await Task.Delay(5000); // Wait for Host to be ready
            await RegisterWithHost(app.Configuration, app.Services.GetRequiredService<ILogger<ProgramApi>>());
        });

        app.Run();
    }

    private static async Task RegisterWithHost(IConfiguration config, ILogger logger)
    {
        try
        {
            var hostUrl = config.GetValue<string>("ServiceUrls:Host") ?? "http://localhost:8080";
            var selfUrl = config.GetValue<string>("ServiceUrls:Self") ?? "http://localhost:8083";
            using var client = new HttpClient { BaseAddress = new Uri(hostUrl) };

            var moduleVersion = config["MODULE_VERSION"]
                ?? config["IMAGE_TAG"]
                ?? config["DOCKER_IMAGE_TAG"]
                ?? "dev";

            var moduleInfo = new ModuleInfoDto
            {
                ModuleName = "Offerte",
                DisplayName = "Offerte",
                Version = moduleVersion,
                LogoUrl = "/offerte/company/logos/Offerte_Logo.png",
                HealthCheckUrl = $"{selfUrl}/offerte/health",
                NavigationItems = new List<NavItemDto>
                {
                    new NavItemDto
                    {
                        Label = "Offerte",
                        Href = "/offerte",
                        Icon = "/offerte/company/logos/Offerte_Logo.png",
                        Type = NavItemType.Link
                    },
                    new NavItemDto
                    {
                        Label = "Angebote",
                        Href = "/offerte/angebote",
                        Icon = "",
                        Type = NavItemType.Link
                    },
                    // Settings: E-Mail-Vorlage unter "Vorlagen"
                    new NavItemDto
                    {
                        Label = "Offerte E-Mail",
                        Href = "/offerte/settings/email-anpassung",
                        Icon = "",
                        Type = NavItemType.Settings,
                        Category = NavSettingsCategory.Vorlagen
                    },
                    // Settings: PDF-Anpassung unter "Dokumente"
                    new NavItemDto
                    {
                        Label = "Offerte PDF",
                        Href = "/offerte/settings/pdf-anpassung",
                        Icon = "",
                        Type = NavItemType.Settings,
                        Category = NavSettingsCategory.Dokumente
                    }
                }
            };

            var response = await client.PostAsJsonAsync("/api/modules/register", moduleInfo);
            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Offerte module registered successfully with Host at {HostUrl}", hostUrl);
            }
            else
            {
                logger.LogWarning("Failed to register Offerte module with Host. Status: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error registering Offerte module with Host");
        }
    }
}
