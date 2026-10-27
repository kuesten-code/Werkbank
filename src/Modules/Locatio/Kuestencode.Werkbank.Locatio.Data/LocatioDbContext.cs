using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Kuestencode.Werkbank.Locatio.Data;

/// <summary>
/// DbContext für die Vermietungsverwaltung. Verwendet das Schema "locatio".
/// </summary>
public class LocatioDbContext : DbContext
{
    public LocatioDbContext(DbContextOptions<LocatioDbContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(RelationalEventId.PendingModelChangesWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("locatio");
    }
}
