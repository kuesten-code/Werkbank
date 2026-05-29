using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kuestencode.Werkbank.Contracta.Data;

public class ContractaDbContextFactory : IDesignTimeDbContextFactory<ContractaDbContext>
{
    public ContractaDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ContractaDbContext>();

        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=contracta_design;Username=postgres;Password=postgres",
            b => b.MigrationsHistoryTable("__EFMigrationsHistory", "contracta"));

        return new ContractaDbContext(optionsBuilder.Options);
    }
}
