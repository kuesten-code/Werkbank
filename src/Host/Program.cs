using Microsoft.AspNetCore.DataProtection;
using MudBlazor.Services;
using Kuestencode.Werkbank.Host;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Services;
using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.Contracts.Navigation;
using QuestPDF.Infrastructure;

// QuestPDF Lizenz konfigurieren
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers();

// Add MudBlazor services
builder.Services.AddMudServices();

// Add Data Protection for password encryption
var keysDirectory = Path.Combine(builder.Environment.ContentRootPath, "data", "keys");
Directory.CreateDirectory(keysDirectory);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysDirectory))
    .SetApplicationName("Kuestencode.Werkbank");

// Add Module Registry
builder.Services.AddSingleton<IModuleRegistry, ModuleRegistry>();
builder.Services.AddSingleton<IHostNavigationService, HostNavigationService>();

// Add HttpClient factory for health checks
builder.Services.AddHttpClient();

// Add Module Health Check Service
builder.Services.AddHostedService<ModuleHealthCheckService>();

// Add Host services (Company, Customer, Email, PDF engines)
builder.Services.AddHostServices(builder.Configuration);

// Add HttpClient für Faktura-API
builder.Services.AddHttpClient<IFakturaApiClient, FakturaApiClient>(client =>
{
    var fakturaUrl = builder.Configuration.GetValue<string>("ServiceUrls:Faktura") ?? "http://localhost:8081";
    client.BaseAddress = new Uri(fakturaUrl);
});

// Add HttpClient fuer Rapport-API
builder.Services.AddHttpClient<IRapportApiClient, RapportApiClient>(client =>
{
    var rapportUrl = builder.Configuration.GetValue<string>("ServiceUrls:Rapport") ?? "http://localhost:8082";
    client.BaseAddress = new Uri(rapportUrl);
});

// Add HttpClient fuer Offerte-API
builder.Services.AddHttpClient<IOfferteApiClient, OfferteApiClient>(client =>
{
    var rapportUrl = builder.Configuration.GetValue<string>("ServiceUrls:Offerte") ?? "http://localhost:8083";
    client.BaseAddress = new Uri(rapportUrl);
});

// Add YARP Reverse Proxy for Faktura, Rapport, and Offerte modules
var fakturaServiceUrl = builder.Configuration.GetValue<string>("ServiceUrls:Faktura") ?? "http://localhost:8081";
var rapportServiceUrl = builder.Configuration.GetValue<string>("ServiceUrls:Rapport") ?? "http://localhost:8082";
var offerteServiceUrl = builder.Configuration.GetValue<string>("ServiceUrls:Offerte") ?? "http://localhost:8083";
builder.Services.AddReverseProxy()
    .LoadFromMemory(
        routes: new[]
        {
            new Yarp.ReverseProxy.Configuration.RouteConfig
            {
                RouteId = "faktura-route",
                ClusterId = "faktura-cluster",
                Match = new Yarp.ReverseProxy.Configuration.RouteMatch
                {
                    Path = "/faktura/{**catch-all}"
                }
            },
            new Yarp.ReverseProxy.Configuration.RouteConfig
            {
                RouteId = "faktura-blazor-route",
                ClusterId = "faktura-cluster",
                Match = new Yarp.ReverseProxy.Configuration.RouteMatch
                {
                    Path = "/_faktura/{**catch-all}"
                }
            },
            new Yarp.ReverseProxy.Configuration.RouteConfig
            {
                RouteId = "rapport-route",
                ClusterId = "rapport-cluster",
                Match = new Yarp.ReverseProxy.Configuration.RouteMatch
                {
                    Path = "/rapport/{**catch-all}"
                }
            },
            new Yarp.ReverseProxy.Configuration.RouteConfig
            {
                RouteId = "rapport-blazor-route",
                ClusterId = "rapport-cluster",
                Match = new Yarp.ReverseProxy.Configuration.RouteMatch
                {
                    Path = "/_rapport/{**catch-all}"
                }
            },
            new Yarp.ReverseProxy.Configuration.RouteConfig
            {
                RouteId = "offerte-route",
                ClusterId = "offerte-cluster",
                Match = new Yarp.ReverseProxy.Configuration.RouteMatch
                {
                    Path = "/offerte/{**catch-all}"
                }
            },
            new Yarp.ReverseProxy.Configuration.RouteConfig
            {
                RouteId = "offerte-blazor-route",
                ClusterId = "offerte-cluster",
                Match = new Yarp.ReverseProxy.Configuration.RouteMatch
                {
                    Path = "/_offerte/{**catch-all}"
                }
            }
        },
        clusters: new[]
        {
            new Yarp.ReverseProxy.Configuration.ClusterConfig
            {
                ClusterId = "faktura-cluster",
                Destinations = new Dictionary<string, Yarp.ReverseProxy.Configuration.DestinationConfig>
                {
                    { "faktura", new Yarp.ReverseProxy.Configuration.DestinationConfig { Address = fakturaServiceUrl } }
                }
            },
            new Yarp.ReverseProxy.Configuration.ClusterConfig
            {
                ClusterId = "rapport-cluster",
                Destinations = new Dictionary<string, Yarp.ReverseProxy.Configuration.DestinationConfig>
                {
                    { "rapport", new Yarp.ReverseProxy.Configuration.DestinationConfig { Address = rapportServiceUrl } }
                }
            },
            new Yarp.ReverseProxy.Configuration.ClusterConfig
            {
                ClusterId = "offerte-cluster",
                Destinations = new Dictionary<string, Yarp.ReverseProxy.Configuration.DestinationConfig>
                {
                    { "offerte", new Yarp.ReverseProxy.Configuration.DestinationConfig { Address = offerteServiceUrl } }
                }
            }
        });

var app = builder.Build();

// Apply migrations
var applyMigrations = builder.Configuration.GetValue("APPLY_MIGRATIONS", true);

if (applyMigrations)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Applying Host database migrations...");
        await app.ApplyMigrationsAsync();
        logger.LogInformation("Host database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying migrations.");
        throw;
    }
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Map Reverse Proxy for Faktura module (before other routes)
app.MapReverseProxy();

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
