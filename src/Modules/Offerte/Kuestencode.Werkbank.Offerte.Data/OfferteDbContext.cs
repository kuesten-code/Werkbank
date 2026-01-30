using Kuestencode.Werkbank.Offerte.Domain.Entities;
using Kuestencode.Werkbank.Offerte.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Kuestencode.Werkbank.Offerte.Data;

/// <summary>
/// DbContext für Offerte-spezifische Daten (Angebote, Positionen).
/// Verwendet das Schema "offerte".
/// </summary>
public class OfferteDbContext : DbContext
{
    public OfferteDbContext(DbContextOptions<OfferteDbContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(w =>
            w.Ignore(RelationalEventId.PendingModelChangesWarning));
    }

    // DbSets
    public DbSet<Angebot> Angebote { get; set; } = null!;
    public DbSet<Angebotsposition> Angebotspositionen { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Schema-Trennung
        modelBuilder.HasDefaultSchema("offerte");

        // Angebot Configuration
        modelBuilder.Entity<Angebot>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.Angebotsnummer).IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Status als String speichern für bessere Lesbarkeit
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            // KundeId bleibt als FK, aber ohne Navigation Property zum Host-Schema
            entity.Property(e => e.KundeId).IsRequired();

            // Relationships innerhalb des Offerte-Schemas
            entity.HasMany(e => e.Positionen)
                .WithOne(e => e.Angebot)
                .HasForeignKey(e => e.AngebotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Angebotsposition Configuration
        modelBuilder.Entity<Angebotsposition>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Menge).HasPrecision(18, 3);
            entity.Property(e => e.Einzelpreis).HasPrecision(18, 2);
            entity.Property(e => e.Steuersatz).HasPrecision(5, 2);
            entity.Property(e => e.Rabatt).HasPrecision(5, 2);
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
