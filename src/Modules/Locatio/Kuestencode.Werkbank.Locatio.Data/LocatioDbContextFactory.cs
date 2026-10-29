using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kuestencode.Werkbank.Locatio.Data;

/// <summary>
/// Factory für die Erstellung des LocatioDbContext zur Design-Zeit (für Migrations).
/// </summary>
public class LocatioDbContextFactory : IDesignTimeDbContextFactory<LocatioDbContext>
{
    public LocatioDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LocatioDbContext>();

        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=werkbank;Username=postgres;Password=postgres",
            b => b.MigrationsHistoryTable("__EFMigrationsHistory", "locatio"));

        return new LocatioDbContext(optionsBuilder.Options);
    }
}
