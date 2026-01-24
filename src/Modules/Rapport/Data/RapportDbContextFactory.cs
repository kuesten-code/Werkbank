using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kuestencode.Rapport.Data;

/// <summary>
/// Factory for creating RapportDbContext at design time (migrations).
/// </summary>
public class RapportDbContextFactory : IDesignTimeDbContextFactory<RapportDbContext>
{
    public RapportDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<RapportDbContext>();

        // Dummy connection string for migrations
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=rapport_design;Username=postgres;Password=postgres",
            b => b.MigrationsHistoryTable("__EFMigrationsHistory", "rapport"));

        return new RapportDbContext(optionsBuilder.Options);
    }
}
