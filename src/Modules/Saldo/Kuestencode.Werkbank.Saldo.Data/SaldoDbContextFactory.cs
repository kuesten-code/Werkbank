using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kuestencode.Werkbank.Saldo.Data;

/// <summary>
/// Factory für die Erstellung des SaldoDbContext zur Design-Zeit (für Migrations).
/// </summary>
public class SaldoDbContextFactory : IDesignTimeDbContextFactory<SaldoDbContext>
{
    public SaldoDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SaldoDbContext>();

        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=werkbank;Username=postgres;Password=postgres",
            b => b.MigrationsHistoryTable("__EFMigrationsHistory", "saldo"));

        return new SaldoDbContext(optionsBuilder.Options);
    }
}
