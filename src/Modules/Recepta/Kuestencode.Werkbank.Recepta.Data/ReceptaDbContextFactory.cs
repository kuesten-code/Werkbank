using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kuestencode.Werkbank.Recepta.Data;

/// <summary>
/// Factory für die Erstellung des ReceptaDbContext zur Design-Zeit (für Migrations).
/// </summary>
public class ReceptaDbContextFactory : IDesignTimeDbContextFactory<ReceptaDbContext>
{
    public ReceptaDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ReceptaDbContext>();

        // Verwende eine Dummy-Connection-String für Migrations
        // Die echte Connection-String kommt aus appsettings.json zur Laufzeit
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=recepta_design;Username=postgres;Password=postgres",
            b => b.MigrationsHistoryTable("__EFMigrationsHistory", "recepta"));

        return new ReceptaDbContext(optionsBuilder.Options);
    }
}
