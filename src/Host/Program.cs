using Microsoft.AspNetCore.DataProtection;
using MudBlazor.Services;
using Kuestencode.Werkbank.Host;
using Kuestencode.Werkbank.Host.Data;
using QuestPDF.Infrastructure;

// QuestPDF Lizenz konfigurieren
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add MudBlazor services
builder.Services.AddMudServices();

// Add Data Protection for password encryption
var keysDirectory = Path.Combine(builder.Environment.ContentRootPath, "data", "keys");
Directory.CreateDirectory(keysDirectory);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysDirectory))
    .SetApplicationName("Kuestencode.Werkbank");

// Add Host services (Company, Customer, Email, PDF engines)
builder.Services.AddHostServices(builder.Configuration);

// TODO: Module laden
// builder.Services.AddFakturaModule(builder.Configuration);

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
        logger.LogError(ex, "An error occurred while applying Host migrations.");
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

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
