using System.Net.Http.Json;
using Kuestencode.Core.Interfaces;
using Kuestencode.Rapport;
using Kuestencode.Rapport.Services;
using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.Contracts.Navigation;
using Microsoft.AspNetCore.DataProtection;
using MudBlazor.Services;
using QuestPDF.Infrastructure;

namespace Kuestencode.Rapport.Api;

/// <summary>
/// Entry point for Rapport when running as a standalone Blazor Server + API service.
/// This is used when Rapport runs in its own Docker container.
/// </summary>
public class ProgramApi
{
    public static async Task Main(string[] args)
    {
        // QuestPDF License configuration
        QuestPDF.Settings.License = LicenseType.Community;

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
                Title = "Rapport API",
                Version = "v1",
                Description = "Rapport management API for Kuestencode Werkbank"
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
            .SetApplicationName("Kuestencode.Rapport.Api");

        // Add Module Registry (stub for API mode - modules are registered via HTTP)
        builder.Services.AddSingleton<IModuleRegistry, ApiModuleRegistry>();

        // Add Host API Client
        builder.Services.AddHttpClient<IHostApiClient, HostApiClient>(client =>
        {
            var hostUrl = builder.Configuration.GetValue<string>("ServiceUrls:Host") ?? "http://localhost:8080";
            client.BaseAddress = new Uri(hostUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Add API-based implementations of Host services (Customer, Company)
        builder.Services.AddScoped<ICustomerService, ApiCustomerService>();
        builder.Services.AddScoped<ICompanyService, ApiCompanyService>();

        // Add Rapport application services
        builder.Services.AddScoped<TimeEntryService>();
        builder.Services.AddScoped<DashboardService>();

        // Add Rapport Services (includes DbContext, Repositories, etc.)
        builder.Services.AddRapportModule(builder.Configuration);

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
                migrationLogger.LogInformation("Applying Rapport database migrations...");
                await RapportModule.ApplyMigrationsAsync(app.Services);
                migrationLogger.LogInformation("Rapport database migrations applied successfully.");
            }
            catch (Exception ex)
            {
                migrationLogger.LogError(ex, "An error occurred while applying Rapport migrations.");
                throw;
            }
        }

        // Configure pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Rapport API v1");
            });
        }

        // Set path base for reverse proxy compatibility
        app.UsePathBase("/rapport");

        app.UseCors();
        app.UseStaticFiles();
        app.UseRouting();

        app.MapRazorPages();

        // Map API Controllers
        app.MapControllers();

        // Health check endpoint for module registry
        app.MapGet("/health", () => Results.Ok(new { status = "healthy", module = "Rapport" }));

        // Map Blazor Hub
        app.MapBlazorHub("/_blazor");

        // Map Rapport pages
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
            var selfUrl = config.GetValue<string>("ServiceUrls:Self") ?? "http://localhost:8082";
            using var client = new HttpClient { BaseAddress = new Uri(hostUrl) };

            var moduleVersion = config["MODULE_VERSION"]
                ?? config["IMAGE_TAG"]
                ?? config["DOCKER_IMAGE_TAG"]
                ?? "dev";

            var moduleInfo = new ModuleInfoDto
            {
                ModuleName = "Rapport",
                DisplayName = "Rapport",
                Version = moduleVersion,
                LogoUrl = "/rapport/company/logos/Rapport_Logo.png",
                HealthCheckUrl = $"{selfUrl}/rapport/health",
                NavigationItems = new List<NavItemDto>
                {
                    new NavItemDto
                    {
                        Label = "Rapport",
                        Href = "/rapport",
                        Icon = "/rapport/company/logos/Rapport_Logo.png",
                        Type = NavItemType.Link
                    },
                    new NavItemDto
                    {
                        Label = "Zeiteinträge",
                        Icon = "",
                        Type = NavItemType.Group,
                        Children = new List<NavItemDto>
                        {
                            new NavItemDto
                            {
                                Label = "Übersicht",
                                Href = "/rapport/time-entries",
                                Icon = "",
                                Type = NavItemType.Link
                            },
                            new NavItemDto
                            {
                                Label = "Manueller Eintrag",
                                Href = "/rapport/time-entries/create",
                                Icon = "",
                                Type = NavItemType.Link
                            }
                        }
                    },
                    new NavItemDto
                    {
                        Label = "Tätigkeitsnachweise",
                        Href = "/rapport/reports",
                        Icon = "",
                        Type = NavItemType.Link
                    },
                    new NavItemDto
                    {
                        Label = "Einstellungen",
                        Href = "/rapport/settings",
                        Icon = "",
                        Type = NavItemType.Link
                    }
                }
            };

            var response = await client.PostAsJsonAsync("/api/modules/register", moduleInfo);
            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Rapport module registered successfully with Host at {HostUrl}", hostUrl);
            }
            else
            {
                logger.LogWarning("Failed to register Rapport module with Host. Status: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error registering Rapport module with Host");
        }
    }
}



















