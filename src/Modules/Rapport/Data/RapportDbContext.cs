using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Kuestencode.Rapport.Data;

/// <summary>
/// DbContext for Rapport-specific data.
/// Uses the schema "rapport".
/// </summary>
public class RapportDbContext : DbContext
{
    public RapportDbContext(DbContextOptions<RapportDbContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Suppress the PendingModelChangesWarning in .NET 9
        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(RelationalEventId.PendingModelChangesWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Schema separation
        modelBuilder.HasDefaultSchema("rapport");
    }
}
