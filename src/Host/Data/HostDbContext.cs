using Kuestencode.Core.Models;
using Kuestencode.Werkbank.Host.Models;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Host.Data;

/// <summary>
/// DbContext für plattformweite Daten (Companies, Customers).
/// Verwendet das Schema "host".
/// </summary>
public class HostDbContext : DbContext
{
    public HostDbContext(DbContextOptions<HostDbContext> options) : base(options)
    {
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Schema-Trennung
        modelBuilder.HasDefaultSchema("host");

        // Company Konfiguration
        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // String-Längen für Enums als int speichern
            entity.Property(e => e.EmailLayout)
                .HasConversion<int>();

            entity.Property(e => e.PdfLayout)
                .HasConversion<int>();
        });

        // Customer Konfiguration
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.CustomerNumber)
                .IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // TeamMember Konfiguration
        modelBuilder.Entity<TeamMember>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.DisplayName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(e => e.Email)
                .HasMaxLength(200);

            entity.HasIndex(e => e.Email)
                .IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        // Team members are stored in Host but don't derive from Core BaseEntity (Guid key),
        // so update timestamps explicitly.
        var teamEntries = ChangeTracker.Entries<TeamMember>();
        foreach (var entry in teamEntries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
