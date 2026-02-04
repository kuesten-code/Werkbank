using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kuestencode.Werkbank.Acta.Data;

/// <summary>
/// Factory für die Erstellung des ActaDbContext zur Design-Zeit (für Migrations).
/// </summary>
public class ActaDbContextFactory : IDesignTimeDbContextFactory<ActaDbContext>
{
    public ActaDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ActaDbContext>();

        // Verwende eine Dummy-Connection-String für Migrations
        // Die echte Connection-String kommt aus appsettings.json zur Laufzeit
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=acta_design;Username=postgres;Password=postgres",
            b => b.MigrationsHistoryTable("__EFMigrationsHistory", "acta"));

        return new ActaDbContext(optionsBuilder.Options);
    }
}
