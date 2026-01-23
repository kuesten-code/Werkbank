using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kuestencode.Faktura.Data;

/// <summary>
/// Factory für die Erstellung des FakturaDbContext zur Design-Zeit (für Migrations).
/// </summary>
public class FakturaDbContextFactory : IDesignTimeDbContextFactory<FakturaDbContext>
{
    public FakturaDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FakturaDbContext>();

        // Verwende eine Dummy-Connection-String für Migrations
        // Die echte Connection-String kommt aus appsettings.json zur Laufzeit
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=faktura_design;Username=postgres;Password=postgres",
            b => b.MigrationsHistoryTable("__EFMigrationsHistory", "faktura"));

        return new FakturaDbContext(optionsBuilder.Options);
    }
}
