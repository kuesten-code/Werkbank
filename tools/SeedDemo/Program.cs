using Kuestencode.Werkbank.Acta.Data;
using Kuestencode.Werkbank.Recepta.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var connectionString = args.Length > 0
    ? args[0]
    : "Host=localhost;Port=5432;Database=kuestencode_dev;Username=postgres;Password=dev_password";

Console.WriteLine($"Verbinde mit: {connectionString}");

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<ReceptaDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddDbContext<ActaDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);

var host = builder.Build();

Console.WriteLine("=== Demo Seed Data ===");
Console.WriteLine();

try
{
    Console.WriteLine("[Acta] Seeding...");
    await Kuestencode.Werkbank.Acta.Data.DemoSeedData.SeedAsync(host.Services);
    Console.WriteLine("[Acta] Fertig.");
}
catch (Exception ex)
{
    Console.WriteLine($"[Acta] FEHLER: {ex.Message}");
}

Console.WriteLine();

try
{
    Console.WriteLine("[Recepta] Seeding...");
    await Kuestencode.Werkbank.Recepta.Data.DemoSeedData.SeedAsync(host.Services);
    Console.WriteLine("[Recepta] Fertig.");
}
catch (Exception ex)
{
    Console.WriteLine($"[Recepta] FEHLER: {ex.Message}");
}

Console.WriteLine();
Console.WriteLine("=== Seed abgeschlossen ===");
