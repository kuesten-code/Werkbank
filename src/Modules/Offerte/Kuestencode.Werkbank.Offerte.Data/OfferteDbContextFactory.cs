using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kuestencode.Werkbank.Offerte.Data;

/// <summary>
/// Factory für die Erstellung des OfferteDbContext zur Design-Zeit (für Migrations).
/// </summary>
public class OfferteDbContextFactory : IDesignTimeDbContextFactory<OfferteDbContext>
{
    public OfferteDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OfferteDbContext>();

        // Verwende eine Dummy-Connection-String für Migrations
        // Die echte Connection-String kommt aus appsettings.json zur Laufzeit
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=offerte_design;Username=postgres;Password=postgres",
            b => b.MigrationsHistoryTable("__EFMigrationsHistory", "offerte"));

        return new OfferteDbContext(optionsBuilder.Options);
    }
}
