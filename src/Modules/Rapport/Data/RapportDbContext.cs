using Kuestencode.Core.Models;
using Kuestencode.Rapport.Models;
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

    public DbSet<TimeEntry> TimeEntries { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Schema separation
        modelBuilder.HasDefaultSchema("rapport");

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers", "host", table => table.ExcludeFromMigrations());
        });

        modelBuilder.Entity<TimeEntry>(entity =>
        {
            entity.HasQueryFilter(e => !e.IsDeleted);
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.StartTime);
            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => new { e.CustomerId, e.ProjectId });
            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CustomerName).HasMaxLength(200);
            entity.Property(e => e.ProjectName).HasMaxLength(200);
            entity.Property(e => e.IsManual).HasDefaultValue(false);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.Status).IsRequired();
        });
    }
}

