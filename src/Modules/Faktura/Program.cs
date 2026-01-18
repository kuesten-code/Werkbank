using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Kuestencode.Faktura;
using Kuestencode.Faktura.Data;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add MudBlazor services
builder.Services.AddMudServices();

// Add Data Protection for password encryption
// Persist keys to a directory that survives container restarts
var keysDirectory = Path.Combine(builder.Environment.ContentRootPath, "data", "keys");
Directory.CreateDirectory(keysDirectory);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysDirectory))
    .SetApplicationName("Kuestencode.Faktura");

// Add all Faktura module services (includes DbContext, Repositories, Services)
builder.Services.AddFakturaModule(builder.Configuration);

var app = builder.Build();

var applyMigrations = builder.Configuration.GetValue("APPLY_MIGRATIONS", true);

if (applyMigrations)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;

    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        logger.LogInformation("Applying database migrations...");
        context.Database.Migrate();
        logger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying migrations.");
        throw;
    }
}
else
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning("APPLY_MIGRATIONS=false -> Skipping Database.Migrate()");
}


// Configure the HTTP request pipeline.
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
