using Kuestencode.Core.Interfaces;
using Kuestencode.Faktura;
using Kuestencode.Faktura.Services;
using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.Contracts.Navigation;
using Microsoft.AspNetCore.DataProtection;
using MudBlazor.Services;
using QuestPDF.Infrastructure;

namespace Kuestencode.Faktura.Api;

/// <summary>
/// Entry point for Faktura when running as a standalone Blazor Server + API service.
/// This is used when Faktura runs in its own Docker container.
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
                Title = "Faktura API",
                Version = "v1",
                Description = "Invoice management API for Kuestencode Werkbank"
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
            .SetApplicationName("Kuestencode.Faktura.Api");

        // Add Module Registry (stub for API mode - modules are registered via HTTP)
        builder.Services.AddSingleton<IModuleRegistry, ApiModuleRegistry>();

        // Add Host API Client (must be registered before API-based services)
        builder.Services.AddHttpClient<IHostApiClient, HostApiClient>(client =>
        {
            var hostUrl = builder.Configuration.GetValue<string>("ServiceUrls:Host") ?? "http://localhost:8080";
            client.BaseAddress = new Uri(hostUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Add API-based implementations of Host services (Company, Customer)
        builder.Services.AddScoped<ICompanyService, ApiCompanyService>();
        builder.Services.AddScoped<ICustomerService, ApiCustomerService>();

        // Add Faktura Services (includes DbContext, Repositories, Email, PDF, etc.)
        builder.Services.AddFakturaModule(builder.Configuration);

        var app = builder.Build();

        // Apply migrations
        var applyMigrations = builder.Configuration.GetValue("APPLY_MIGRATIONS", true);
        if (applyMigrations)
        {
            var logger = app.Services.GetRequiredService<ILogger<ProgramApi>>();
            try
            {
                logger.LogInformation("Applying Faktura database migrations...");
                await FakturaModule.ApplyMigrationsAsync(app.Services);
                logger.LogInformation("Faktura database migrations applied successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while applying Faktura migrations.");
                throw;
            }
        }

        // Configure pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Faktura API v1");
            });
        }

        // Set path base for reverse proxy compatibility
        // This ensures static files and routes work correctly when accessed via /faktura prefix
        app.UsePathBase("/faktura");

        app.UseCors();
        app.UseStaticFiles();
        app.UseRouting();

        // Map API Controllers
        app.MapControllers();

        // Health check endpoint for module registry
        app.MapGet("/health", () => Results.Ok(new { status = "healthy", module = "Faktura" }));

        // Map Blazor Hub (path relative to path base)
        app.MapBlazorHub("/_blazor");

        // Map Faktura pages (paths are relative to path base /faktura)
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
            var selfUrl = config.GetValue<string>("ServiceUrls:Self") ?? "http://localhost:8081";
            using var client = new HttpClient { BaseAddress = new Uri(hostUrl) };

            var moduleInfo = new ModuleInfoDto
            {
                ModuleName = "Faktura",
                DisplayName = "Faktura (Rechnungen)",
                Version = "1.0.0",
                HealthCheckUrl = $"{selfUrl}/faktura/health",
                NavigationItems = new List<NavItemDto>
                {
                    new NavItemDto
                    {
                        Label = "Faktura",
                        Href = "/faktura",
                        Icon = "Dashboard",
                        Type = NavItemType.Link
                    },
                    new NavItemDto
                    {
                        Label = "Rechnungen",
                        Href = "/faktura/invoices",
                        Icon = "Receipt",
                        Type = NavItemType.Link
                    },
                    new NavItemDto
                    {
                        Label = "Faktura Einstellungen",
                        Icon = "Settings",
                        Type = NavItemType.Group,
                        Children = new List<NavItemDto>
                        {
                            new NavItemDto
                            {
                                Label = "E-Mail-Anpassung",
                                Href = "/faktura/settings/email-anpassung",
                                Icon = "Palette",
                                Type = NavItemType.Link
                            },
                            new NavItemDto
                            {
                                Label = "PDF-Anpassung",
                                Href = "/faktura/settings/pdf-anpassung",
                                Icon = "PictureAsPdf",
                                Type = NavItemType.Link
                            }
                        }
                    }
                }
            };

            var response = await client.PostAsJsonAsync("/api/modules/register", moduleInfo);
            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Faktura module registered successfully with Host at {HostUrl}", hostUrl);
            }
            else
            {
                logger.LogWarning("Failed to register Faktura module with Host. Status: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error registering Faktura module with Host");
        }
    }
}
