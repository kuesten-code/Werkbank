using Kuestencode.Werkbank.Acta.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Kuestencode.Werkbank.Acta.Data;

/// <summary>
/// DbContext für Acta-spezifische Daten (Projekte, Aufgaben).
/// Verwendet das Schema "acta".
/// </summary>
public class ActaDbContext : DbContext
{
    public ActaDbContext(DbContextOptions<ActaDbContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(RelationalEventId.PendingModelChangesWarning));
    }

    // DbSets
    public DbSet<Project> Projects { get; set; } = null!;
    public DbSet<ProjectTask> Tasks { get; set; } = null!;
    public DbSet<ProjektStundensatz> ProjektStundensaetze { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Schema-Trennung
        modelBuilder.HasDefaultSchema("acta");

        // Project Configuration
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.ProjectNumber).IsUnique();
            entity.HasIndex(e => e.ExternalId).IsUnique().HasFilter("\"ExternalId\" IS NOT NULL");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Status als String speichern für bessere Lesbarkeit
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            // CustomerId bleibt als FK, aber ohne Navigation Property zum Host-Schema
            entity.Property(e => e.CustomerId).IsRequired();

            // Budget Precision
            entity.Property(e => e.BudgetNet).HasPrecision(18, 2);

            entity.Property(e => e.MaterialBerechnedNetto).HasPrecision(18, 2).HasDefaultValue(0m);
            entity.Property(e => e.MaterialBerechnedBrutto).HasPrecision(18, 2).HasDefaultValue(0m);

            // Relationships innerhalb des Acta-Schemas
            entity.HasMany(e => e.Tasks)
                .WithOne(e => e.Project)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ProjektStundensatz Configuration
        modelBuilder.Entity<ProjektStundensatz>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.RolleId).IsRequired();
            entity.Property(e => e.RolleName).HasMaxLength(100).IsRequired();

            entity.Property(e => e.Stundensatz)
                .HasPrecision(10, 2)
                .IsRequired();

            entity.Property(e => e.ErstelltAm)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => new { e.ProjectId, e.RolleId })
                .IsUnique();

            entity.HasOne(e => e.Project)
                .WithMany()
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ProjectTask Configuration
        modelBuilder.Entity<ProjectTask>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Status als String speichern für bessere Lesbarkeit
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            // AssignedUserId bleibt als FK, aber ohne Navigation Property zum Host-Schema
            entity.Property(e => e.AssignedUserId).IsRequired(false);
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity.GetType().GetProperty("UpdatedAt") != null)
            {
                entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
            }

            if (entry.State == EntityState.Added && entry.Entity.GetType().GetProperty("CreatedAt") != null)
            {
                entry.Property("CreatedAt").CurrentValue = DateTime.UtcNow;
            }
        }
    }
}
