using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Kuestencode.Werkbank.Locatio.Tests;

/// <summary>
/// Minimale WebApplicationFactory nur für HTTP-Ebenen-Tests (z.B. Autorisierung), die vor jedem
/// DB-/externem Service-Zugriff greifen ([RequireRole] läuft als Authorization-Filter, bevor der
/// Controller instanziiert wird). Migrations werden übersprungen, da für diese Tests nie eine
/// echte Datenbankverbindung nötig ist.
/// </summary>
public sealed class LocatioWebApplicationFactory : WebApplicationFactory<ProgramApi>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["APPLY_MIGRATIONS"] = "false"
            });
        });
    }
}
